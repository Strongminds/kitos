namespace PubSub.Application.DTOs
{
    public record SubscriptionRequestDto
    {
        public required string Callback { get; set; }
        public required List<string> Queues { get; set; }
    }
}
