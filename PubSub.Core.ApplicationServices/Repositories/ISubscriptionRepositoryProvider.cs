namespace PubSub.Core.ApplicationServices.Repositories
{
    public interface ISubscriptionRepositoryProvider
    {
        ISubscriptionRepository Get();
    }
}
