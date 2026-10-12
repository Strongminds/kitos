namespace PubSub.Core.DomainModel.UserSync;

/// <summary>Legacy outbox retained for restartable cutover and audit; never polled for delivery.</summary>
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
    public DateTime? RabbitMqMigratedAt { get; set; }
}
