namespace PubSub.Application
{
    public class Subscription
    {
        public string Callback { get; set; }
        public IEnumerable<string> Queues { get; set; }
    }
}
