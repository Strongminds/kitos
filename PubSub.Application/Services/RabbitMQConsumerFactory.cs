using PubSub.Application;
using PubSub.Core.ApplicationServices.Notifier;
using PubSub.Core.ApplicationServices.Serializer;
using PubSub.Core.DomainServices.RabbitMQConnection;
using PubSub.Core.DomainServices.Subscriber;

namespace PubSub.Application.Services
{
    public class RabbitMQConsumerFactory : IRabbitMQConsumerFactory
    {
        public IConsumer Create(IRabbitMQConnectionManager connectionManager, ISubscriberNotifierService subscriberNotifierService, IPayloadSerializer payloadSerializer, string topic, IServiceScopeFactory scopeFactory)
        {
            return new RabbitMQConsumer(connectionManager, subscriberNotifierService, payloadSerializer, topic, scopeFactory);
        }
    }
}
