namespace PubSub.Application
{
    public class SubscriptionRequestDto
    {
        public string Callback { get; set; }
        public List<string> Queues { get; set; }
    }
}
