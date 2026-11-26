#nullable enable
using System;

namespace Core.DomainModel.Users;

public enum ExternalUserChangeType { Deleted = 1 }
public enum ExternalUserChangeStatus { Pending = 0, Applied = 1, Dismissed = 2 }

/// <summary>An external fact awaiting an explicit local administrator decision.</summary>
public class ExternalUserChange
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string ExternalMessageId { get; set; } = string.Empty;
    public int OrganizationId { get; set; }
    public virtual Organization.Organization Organization { get; set; } = null!;
    public Guid ExternalUserUuid { get; set; }
    public int? UserId { get; set; }
    public virtual User? User { get; set; }
    public ExternalUserChangeType ChangeType { get; set; }
    public ExternalUserChangeStatus Status { get; set; }
    public DateTime? OccurredAt { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedByUserId { get; set; }
    public virtual User? ResolvedByUser { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}
