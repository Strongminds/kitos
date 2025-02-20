namespace PubSub.Application
{
    public interface ISubscribeLoopHostedService: IHostedService
    {
        void UpdateSubscriptions(IList<Subscription> subscriptions);
    }
}
