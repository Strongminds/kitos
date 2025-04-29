namespace PubSub.Core.DomainServices.Subscriber
{
    public interface IConsumer : IDisposable
    {
        Task StartListeningAsync();
    }
}
