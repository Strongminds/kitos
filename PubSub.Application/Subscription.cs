namespace PubSub.Application
{
    public class Subscription
    {
        public string Callback { get; set; }
        public IEnumerable<Topic> Topics { get; set; }
    }
}
