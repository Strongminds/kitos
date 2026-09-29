using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Abstractions.Extensions;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Model.Users;
using Core.DomainModel;
using Core.DomainModel.Events;
using Core.DomainModel.Organization;
using Core.DomainModel.Organization.DomainEvents;
using Core.DomainModel.Users;
using Core.DomainServices;
using Core.DomainServices.Repositories.SSO;
using Core.DomainServices.Users;

namespace Core.ApplicationServices.Users;

public class ExternalUserChangeResolutionService(
    IExternalUserChangeResolutionTransaction transaction,
    IGenericRepository<Organization> organizations,
    IGenericRepository<ExternalUserChange> changes,
    IGenericRepository<OrganizationRight> rights,
    IOrganizationalUserContext actor,
    ISsoUserIdentityRepository identities,
    IDomainEvents events)
{
    public IReadOnlyList<ExternalUserChangeBatchResult> ResolveBatch(
        Guid organizationUuid, IEnumerable<Guid> changeUuids, bool apply, CancellationToken cancellationToken)
    {
        var results = new List<ExternalUserChangeBatchResult>();
        foreach (var uuid in changeUuids.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Each item has its own transaction and must reload state after the previous resolution.
            transaction.ClearTracking();
            results.Add(Resolve(organizationUuid, uuid, apply).Match(
                _ => new ExternalUserChangeBatchResult(uuid, true, null),
                error => new ExternalUserChangeBatchResult(uuid, false,
                    error.Message.GetValueOrFallback(error.FailureType.ToString()))));
        }
        return results;
    }

    public Result<ExternalUserChange, OperationError> Resolve(Guid organizationUuid, Guid changeUuid, bool apply)
    {
        var result = transaction.Execute(() => ResolveWithinTransaction(organizationUuid, changeUuid, apply));
        if (apply && result.Ok)
            events.Raise(new AdministrativeAccessRightsChanged(result.Value.UserId!.Value));
        return result;
    }

    private Result<ExternalUserChange, OperationError> ResolveWithinTransaction(Guid organizationUuid, Guid changeUuid, bool apply) =>
        GetAuthorizedOrganization(organizationUuid)
            .Bind(organization => GetPendingChange(organization, changeUuid)
                .Bind(change => apply ? ApplyDeletion(organization, change) : SaveResolution(change, false)));

    private Result<Organization, OperationError> GetAuthorizedOrganization(Guid organizationUuid) =>
        organizations.AsQueryable().SingleOrDefault(x => x.Uuid == organizationUuid).FromNullable()
            .Match<Result<Organization, OperationError>>(
                organization => actor.HasRole(organization.Id, OrganizationRole.LocalAdmin)
                    ? organization
                    : new OperationError("Local administrator permission required.", OperationFailure.Forbidden),
                () => new OperationError("Organization not found.", OperationFailure.NotFound));

    private Result<ExternalUserChange, OperationError> GetPendingChange(Organization organization, Guid changeUuid) =>
        changes.AsQueryable().SingleOrDefault(x => x.Uuid == changeUuid && x.OrganizationId == organization.Id)
            .FromNullable()
            .Match<Result<ExternalUserChange, OperationError>>(
                change => change.Status == ExternalUserChangeStatus.Pending
                    ? change
                    : new OperationError("Change has already been resolved.", OperationFailure.Conflict),
                () => new OperationError("Change not found.", OperationFailure.NotFound));

    private Result<ExternalUserChange, OperationError> ApplyDeletion(Organization organization, ExternalUserChange change)
    {
        if (change.ChangeType != ExternalUserChangeType.Deleted)
            return new OperationError("Unsupported change type.", OperationFailure.BadInput);

        return identities.GetByExternalUuid(change.ExternalUserUuid)
            .Match<Result<ExternalUserChange, OperationError>>(
                identity => ValidateTarget(organization, change, identity.User)
                    .Bind(user => RemoveOrganizationAccess(organization, change, user)),
                () => new OperationError("No SSO identity binding exists.", OperationFailure.Conflict));
    }

    private Result<User, OperationError> ValidateTarget(Organization organization, ExternalUserChange change, User user)
    {
        var isMember = rights.AsQueryable().Any(x => x.UserId == user.Id && x.OrganizationId == organization.Id &&
            x.Role == OrganizationRole.User);
        if (!isMember || (change.UserId.HasValue && change.UserId != user.Id))
            return new OperationError("The identity no longer matches a member of this organization.", OperationFailure.Conflict);
        if (user.IsGlobalAdmin || rights.AsQueryable().Any(x => x.UserId == user.Id && x.Role == OrganizationRole.GlobalAdmin))
            return new OperationError("Global administrators require global access administration.", OperationFailure.Conflict);
        return user;
    }

    private Result<ExternalUserChange, OperationError> RemoveOrganizationAccess(Organization organization, ExternalUserChange change, User user)
    {
        change.User = user;
        change.UserId = user.Id;
        rights.RemoveRange(rights.AsQueryable().Where(x => x.UserId == user.Id && x.OrganizationId == organization.Id).ToList());
        if (!rights.AsQueryable().Any(x => x.UserId == user.Id &&
            x.OrganizationId != organization.Id && x.Role == OrganizationRole.User))
        {
            user.Deleted = true;
            user.DeletedDate = DateTime.UtcNow;
        }
        return SaveResolution(change, true);
    }

    private Result<ExternalUserChange, OperationError> SaveResolution(ExternalUserChange change, bool apply)
    {
        change.Status = apply ? ExternalUserChangeStatus.Applied : ExternalUserChangeStatus.Dismissed;
        change.ResolvedAt = DateTime.UtcNow;
        change.ResolvedByUserId = actor.UserId;
        change.Version = Guid.NewGuid();
        changes.Save();
        return change;
    }
}
