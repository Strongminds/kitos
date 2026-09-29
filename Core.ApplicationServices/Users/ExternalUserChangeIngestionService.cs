using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions.Types;
using Core.ApplicationServices.Model.Users;
using Core.DomainModel.Users;
using Core.DomainModel.Organization;
using Core.DomainServices.Repositories.Organization;
using Core.DomainServices.Repositories.SSO;
using Core.DomainServices.Users;

namespace Core.ApplicationServices.Users;

public class ExternalUserChangeIngestionService(
    IExternalUserChangeRepository repository,
    IOrganizationRepository organizations,
    ISsoUserIdentityRepository identities)
{
    public async Task<Result<ExternalUserChange, OperationError>> Ingest(
        ExternalUserChangeInput input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.ExternalMessageId) || input.ExternalMessageId.Length > 200 ||
            input.OrganizationUuid == Guid.Empty || input.ExternalUserUuid == Guid.Empty ||
            input.ChangeType != ExternalUserChangeType.Deleted ||
            (input.OccurredAt.HasValue && input.OccurredAt.Value.Kind != DateTimeKind.Utc))
            return new OperationError("Invalid deletion event. Supply identifiers and an optional UTC occurrence time.", OperationFailure.BadInput);

        var organization = organizations.GetByUuid(input.OrganizationUuid);
        if (organization.IsNone) return new OperationError("Unknown organization.", OperationFailure.NotFound);

        var existing = await repository.FindByMessageId(input.ExternalMessageId, cancellationToken);
        if (existing != null) return CheckDuplicate(existing, input, organization.Value.Id);

        var identity = identities.GetByExternalUuid(input.ExternalUserUuid);
        var user = identity.HasValue ? identity.Value.User : null;
        var matched = user != null && user.OrganizationRights.Any(x => x.OrganizationId == organization.Value.Id && x.Role == OrganizationRole.User);
        var change = new ExternalUserChange
        {
            ExternalMessageId = input.ExternalMessageId,
            OrganizationId = organization.Value.Id,
            ExternalUserUuid = input.ExternalUserUuid,
            UserId = matched ? user!.Id : null,
            ChangeType = input.ChangeType,
            Status = ExternalUserChangeStatus.Pending,
            OccurredAt = NormalizeTimestamp(input.OccurredAt),
            ReceivedAt = DateTime.UtcNow
        };
        var saved = await repository.Insert(change, cancellationToken);
        return CheckDuplicate(saved, input, organization.Value.Id);
    }

    private static Result<ExternalUserChange, OperationError> CheckDuplicate(
        ExternalUserChange existing, ExternalUserChangeInput input, int organizationId) =>
        existing.OrganizationId == organizationId && existing.ExternalUserUuid == input.ExternalUserUuid &&
        existing.ChangeType == input.ChangeType && NormalizeTimestamp(existing.OccurredAt) == NormalizeTimestamp(input.OccurredAt)
            ? existing
            : new OperationError("Message ID already belongs to a different event.", OperationFailure.Conflict);

    // PostgreSQL timestamps retain microseconds, whereas .NET timestamps can include 100ns ticks.
    // Normalize before persistence and comparison so an identical replay survives a database round trip.
    private static DateTime? NormalizeTimestamp(DateTime? value) => value.HasValue
        ? new DateTime(value.Value.Ticks - value.Value.Ticks % 10, DateTimeKind.Utc)
        : null;
}
