namespace Tests.PubSubTester.DTOs
{
    public class SubscriptionDTO
    {
        public string Callback { get; set; }
        public List<string> Queues { get; set; }
    }
}
