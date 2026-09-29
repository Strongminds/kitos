using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.DomainModel.Events;
using Core.DomainModel.Organization;
using Core.DomainModel.Organization.DomainEvents;
using Core.DomainModel.Users;
using Core.DomainServices.Repositories.SSO;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Presentation.Web.Services;

public class ExternalUserChangeResolutionService(KitosContext db, IOrganizationalUserContext actor,
    ISsoUserIdentityRepository identities, IDomainEvents events)
{
    public async Task<Result<ExternalUserChange, OperationError>> Resolve(Guid organizationUuid, Guid changeUuid,
        bool apply, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var organization = await db.Organizations.SingleOrDefaultAsync(x => x.Uuid == organizationUuid, cancellationToken);
            if (organization == null) return new OperationError("Organization not found.", OperationFailure.NotFound);
            if (!actor.HasRole(organization.Id, OrganizationRole.LocalAdmin))
                return new OperationError("Local administrator permission required.", OperationFailure.Forbidden);
            var change = await db.Set<ExternalUserChange>().Include(x => x.User).Include(x => x.ResolvedByUser)
                .SingleOrDefaultAsync(x => x.Uuid == changeUuid && x.OrganizationId == organization.Id, cancellationToken);
            if (change == null) return new OperationError("Change not found.", OperationFailure.NotFound);
            if (change.Status != ExternalUserChangeStatus.Pending)
                return new OperationError("Change has already been resolved.", OperationFailure.Conflict);
            if (apply)
            {
                if (change.ChangeType != ExternalUserChangeType.Deleted)
                    return new OperationError("Unsupported change type.", OperationFailure.BadInput);
                var identity = identities.GetByExternalUuid(change.ExternalUserUuid);
                if (identity.IsNone) return new OperationError("No SSO identity binding exists.", OperationFailure.Conflict);
                var user = identity.Value.User;
                var member = await db.OrganizationRights
                    .AnyAsync(x => x.UserId == user.Id && x.OrganizationId == organization.Id && x.Role == OrganizationRole.User, cancellationToken);
                if (!member || (change.UserId.HasValue && change.UserId != user.Id))
                    return new OperationError("The identity no longer matches a member of this organization.", OperationFailure.Conflict);
                // A local administrator cannot revoke global privileges through an organization-specific event.
                if (user.IsGlobalAdmin || await db.OrganizationRights
                    .AnyAsync(x => x.UserId == user.Id && x.Role == OrganizationRole.GlobalAdmin, cancellationToken))
                    return new OperationError("Global administrators require global access administration.", OperationFailure.Conflict);
                change.User = user;
                change.UserId = user.Id;
                var organizationRights = await db.OrganizationRights
                    .Where(x => x.UserId == user.Id && x.OrganizationId == organization.Id)
                    .ToListAsync(cancellationToken);
                db.OrganizationRights.RemoveRange(organizationRights);
                if (!await db.OrganizationRights.AnyAsync(x => x.UserId == user.Id &&
                    x.OrganizationId != organization.Id && x.Role == OrganizationRole.User, cancellationToken))
                {
                    user.Deleted = true;
                    user.DeletedDate = DateTime.UtcNow;
                }

            }
            change.Status = apply ? ExternalUserChangeStatus.Applied : ExternalUserChangeStatus.Dismissed;
            change.ResolvedAt = DateTime.UtcNow;
            change.ResolvedByUserId = actor.UserId;
            change.Version = Guid.NewGuid();
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            if (apply) events.Raise(new AdministrativeAccessRightsChanged(change.UserId!.Value));
            return change;
        }
        catch (Exception ex) when (IsConcurrentResolution(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return new OperationError("Concurrent resolution. Reload the change before retrying.", OperationFailure.Conflict);
        }
    }

    private static bool IsConcurrentResolution(Exception exception)
    {
        // Npgsql's execution strategy may wrap serialization failures in
        // InvalidOperationException, outside EF's DbUpdateException.
        for (var current = exception; current != null; current = current.InnerException)
            if (current is DbUpdateConcurrencyException ||
                current is PostgresException { SqlState: "40001" or "23505" })
                return true;
        return false;
    }
}
