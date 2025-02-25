using AutoFixture;
using Moq;
using PubSub.Application;
using RabbitMQ.Client;
using System.Threading.Tasks;

namespace PubSub.Test.Unit
{
    public class RabbitMQMessageBusTopicManagerTest
    {
        [Fact]
        public async Task Declares_Queue_On_Channel()
        {
            var fixture = new Fixture();
            var channel = new Mock<IChannel>();
            var sut = new RabbitMQMessageBusTopicManager(channel.Object);
            var topic = new Topic() { Name = fixture.Create<string>()};

            await sut.Add(topic);

            channel.Verify(_ => _.QueueDeclareAsync(topic.Name, It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<IDictionary<string, object?>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));

        }
    }
}
