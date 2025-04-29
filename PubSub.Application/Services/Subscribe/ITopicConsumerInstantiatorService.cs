namespace PubSub.Application.Services.Consumer
{
    public interface ITopicConsumerInstantiatorService
    {
        Task InstantiateTopic(string topic);
    }
}
