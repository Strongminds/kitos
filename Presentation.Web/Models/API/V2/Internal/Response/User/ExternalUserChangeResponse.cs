using System;
using Core.DomainModel.Users;

namespace Presentation.Web.Models.API.V2.Internal.Response.User;

public record ExternalUserChangeResponse(Guid Uuid, string ExternalMessageId, Guid ExternalUserUuid,
    Guid? UserUuid, string? UserName, string? Email, ExternalUserChangeType ChangeType, ExternalUserChangeStatus Status,
    DateTime? OccurredAt, DateTime ReceivedAt, DateTime? ResolvedAt, Guid? ResolvedByUserUuid)
{
    public static ExternalUserChangeResponse From(ExternalUserChange change) => new(
        change.Uuid, change.ExternalMessageId, change.ExternalUserUuid, change.User?.Uuid, change.User?.GetFullName(), change.User?.Email,
        change.ChangeType, change.Status, change.OccurredAt, change.ReceivedAt,
        change.ResolvedAt, change.ResolvedByUser?.Uuid);
}
