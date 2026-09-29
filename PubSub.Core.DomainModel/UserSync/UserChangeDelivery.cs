namespace PubSub.Core.DomainModel.UserSync;

/// <summary>Durable handoff: upstream may acknowledge only after this record is committed.</summary>
public class UserChangeDelivery
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string ExternalMessageId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public int Attempts { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public bool DeadLettered { get; set; }
    public int? LastStatusCode { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}
