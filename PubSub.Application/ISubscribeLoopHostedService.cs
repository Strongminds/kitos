namespace PubSub.Application
{
    public interface ISubscribeLoopHostedService: IHostedService
    {
        Task UpdateSubscriptions(IEnumerable<Subscription> subscriptions);
    }
}
