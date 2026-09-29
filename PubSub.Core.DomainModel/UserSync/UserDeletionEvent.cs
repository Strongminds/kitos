namespace PubSub.Core.DomainModel.UserSync;

/// <summary>Normalized KITOS contract. This is not the Beskedfordeler wire format.</summary>
public record UserDeletionEvent(string ExternalMessageId, Guid OrganizationUuid,
    Guid ExternalUserUuid, int ChangeType = 1, DateTime? OccurredAt = null)
{
    public bool IsValid() => !string.IsNullOrWhiteSpace(ExternalMessageId) && ExternalMessageId.Length <= 200 &&
        OrganizationUuid != Guid.Empty && ExternalUserUuid != Guid.Empty && ChangeType == 1 &&
        (!OccurredAt.HasValue || OccurredAt.Value.Kind == DateTimeKind.Utc);
}
