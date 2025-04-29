namespace PubSub.Application.Services.Consumer
{
    public interface IConsumer : IDisposable
    {
        Task StartListeningAsync();
    }
}
