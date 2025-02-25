namespace PubSub.Application.Subscribe
{
    public interface ISubscribeLoopHostedService: IHostedService
    {
        Task UpdateSubscriptions(IEnumerable<Subscription> subscriptions);
    }
}
