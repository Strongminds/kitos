namespace PubSub.Application.Services
{
    public interface ISubscriberService
    {
        Task AddSubscriptionsAsync(string topic);
    }
}
