namespace PubSub.Core.Services.Notifier
{
    public interface ISubscriberNotifierService
    {
        public Task Notify(string message, string recipient);
    }
}
