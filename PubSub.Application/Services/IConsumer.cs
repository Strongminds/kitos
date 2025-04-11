namespace PubSub.Application.Services
{
    public interface IConsumer: IDisposable
    {
        Task StartListeningAsync();
        void AddCallbackUrl(Uri callbackUrl);
    }
}
