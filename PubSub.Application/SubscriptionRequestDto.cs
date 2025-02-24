namespace PubSub.Application
{
    public class SubscriptionRequestDto
    {
        public required string Callback { get; set; }
        public required List<string> Queues { get; set; }
    }
}
