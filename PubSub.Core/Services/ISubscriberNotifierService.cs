namespace PubSub.Core.Services
{
    public interface ISubscriberNotifierService
    {
        public Task Notify(string message, string recipient);
    }
}
