using AutoFixture;
using Moq;
using PubSub.Core.Managers;
using PubSub.Core.Models;
using PubSub.Core.Services.Publish;
using PubSub.Core.Services.Serializer;
using RabbitMQ.Client;

namespace PubSub.Test.Unit.Core
{
    public class MockBasicProperties : IReadOnlyBasicProperties, IAmqpHeader
    {
        // Implement members of both interfaces
        public ushort ProtocolClassId => throw new NotImplementedException();

        public string? AppId => throw new NotImplementedException();

        public string? ClusterId => throw new NotImplementedException();

        public string? ContentEncoding => throw new NotImplementedException();

        public string? ContentType => throw new NotImplementedException();

        public string? CorrelationId => throw new NotImplementedException();

        public DeliveryModes DeliveryMode => throw new NotImplementedException();

        public string? Expiration => throw new NotImplementedException();

        public IDictionary<string, object?>? Headers => throw new NotImplementedException();

        public string? MessageId => throw new NotImplementedException();

        public bool Persistent => throw new NotImplementedException();

        public byte Priority => throw new NotImplementedException();

        public string? ReplyTo => throw new NotImplementedException();

        public PublicationAddress? ReplyToAddress => throw new NotImplementedException();

        public AmqpTimestamp Timestamp => throw new NotImplementedException();

        public string? Type => throw new NotImplementedException();

        public string? UserId => throw new NotImplementedException();

        public int GetRequiredBufferSize()
        {
            throw new NotImplementedException();
        }

        public bool IsAppIdPresent()
        {
            throw new NotImplementedException();
        }

        public bool IsClusterIdPresent()
        {
            throw new NotImplementedException();
        }

        public bool IsContentEncodingPresent()
        {
            throw new NotImplementedException();
        }

        public bool IsContentTypePresent()
        {
            throw new NotImplementedException();
        }

        public bool IsCorrelationIdPresent()
        {
            throw new NotImplementedException();
        }

        public bool IsDeliveryModePresent()
        {
            throw new NotImplementedException();
        }

        public bool IsExpirationPresent()
        {
            throw new NotImplementedException();
        }

        public bool IsHeadersPresent()
        {
            throw new NotImplementedException();
        }

        public bool IsMessageIdPresent()
        {
            throw new NotImplementedException();
        }

        public bool IsPriorityPresent()
        {
            throw new NotImplementedException();
        }

        public bool IsReplyToPresent()
        {
            throw new NotImplementedException();
        }

        public bool IsTimestampPresent()
        {
            throw new NotImplementedException();
        }

        public bool IsTypePresent()
        {
            throw new NotImplementedException();
        }

        public bool IsUserIdPresent()
        {
            throw new NotImplementedException();
        }

        public int WriteTo(Span<byte> span)
        {
            throw new NotImplementedException();
        }
    }

    public class RabbitMQPublisherServiceTest
    {
        [Fact]
        public async Task Can_Publish_On_Queue()
        {
            var fixture = new Fixture();
            var connectionManager = new Mock<IConnectionManager>();
            var connection = new Mock<IConnection>();
            var channel = new Mock<IChannel>();
            connectionManager.Setup(_ => _.GetConnectionAsync()).ReturnsAsync(connection.Object);
            connection.Setup(_ => _.CreateChannelAsync(null, default)).ReturnsAsync(channel.Object);

            var messageSerializer = new Mock<IMessageSerializer>();

            var sut = new RabbitMQPublisherService(connectionManager.Object, messageSerializer.Object);
            var publication = fixture.Create<Publication>();

            await sut.Publish(publication);
            channel.Verify(ch => ch.QueueDeclareAsync(
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<IDictionary<string, object?>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()
            ), Times.Once);
            //TODO 26/2/25 need to also verify BasicPublishAsync() which is difficult due to the double interface requirements on its TProperties
        }
    }
}
