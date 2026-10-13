namespace PubSub.Core.DomainModel.UserSync;

/// <summary>Identity ledger only. Pending receipts require source redelivery or operator replay.</summary>
public class UserChangeReceipt
{
    public string ExternalMessageId { get; set; } = string.Empty;
    public string EventHash { get; set; } = string.Empty;
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
}
