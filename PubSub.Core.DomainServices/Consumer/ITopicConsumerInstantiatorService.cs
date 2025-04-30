namespace PubSub.Core.DomainServices.Consumer
{
    public interface ITopicConsumerInstantiatorService
    {
        Task InstantiateTopic(string topic);
    }
}
