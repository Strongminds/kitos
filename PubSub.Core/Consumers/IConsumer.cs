namespace PubSub.Core.Consumers
{
    interface IConsumer: IDisposable
    {
        Task StartListeningAsync();
    }
}
