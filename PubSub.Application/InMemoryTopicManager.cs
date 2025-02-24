
namespace PubSub.Application
{
    public class InMemoryTopicManager : ITopicManager
    {
        private readonly Dictionary<Topic, IEnumerable<string>> topics = new Dictionary<Topic, IEnumerable<string>>();

        public async Task Add(IEnumerable<Subscription> subscriptions)
        {
            throw new NotImplementedException();
        }

        public Dictionary<Topic, IEnumerable<string>> Get()
        {
            throw new NotImplementedException();
        }
    }
}
