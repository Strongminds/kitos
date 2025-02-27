namespace PubSub.Core.Models
{
    public class Subscription
    {
        public string Callback { get; set; }
        public IEnumerable<Topic> Topics { get; set; }
    }
}
