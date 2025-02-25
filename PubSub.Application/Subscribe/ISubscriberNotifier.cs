namespace PubSub.Application.Subscribe
{
    public interface ISubscriberNotifier
    {
        public Task Notify(string message, string recipient);
    }
}
