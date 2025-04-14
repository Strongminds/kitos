namespace PubSub.Core.Models
{
    public class Subscription
    {
        protected Subscription()
        {
            Uuid = Guid.NewGuid();
        }
        public Subscription(string callback, string topicName)
        {
            Uuid = Guid.NewGuid();
            Callback = callback;
            Topic = topicName;
        }
        public Guid Uuid { get; set; }
        public string Callback { get; set; }
        public string Topic { get; set; }
        public string OwnerId { get; set; }
    }
}
