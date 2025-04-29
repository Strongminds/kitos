namespace PubSub.Core.DomainServices.Subscriber
{
    public interface ITopicConsumerInstantiatorService
    {
        Task InstantiateTopic(string topic);
    }
}
