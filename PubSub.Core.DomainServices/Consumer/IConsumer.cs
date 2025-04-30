namespace PubSub.Core.DomainServices.Consumer
{
    public interface IConsumer : IDisposable
    {
        Task StartListeningAsync();
    }
}
