using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using Tools.Beskedfordeler;
using Xunit;

namespace Tests.Unit.Presentation.Web.Infrastructure
{
    public class BeskedfordelerSenderTest
    {
        [Theory]
        [InlineData("<StandardRetur xmlns=\"urn:oio:sagdok:3.0.0\"><StatusKode>20</StatusKode><StatusTekst>Accepted</StatusTekst></StandardRetur>")]
        [InlineData("<StandardRetur xmlns=\"urn:oio:sagdok:3.0.0\">\n<StatusKode>41</StatusKode><StatusTekst>Token expired</StatusTekst>\n</StandardRetur>")]
        [InlineData("Malformed response without a status code")]
        public async Task Publish_CorrelatedReply_ReturnsEntireUnmodifiedResponse(string reply)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var channel = new Mock<IChannel>();
            IAsyncBasicConsumer? consumer = null;
            channel.Setup(x => x.BasicConsumeAsync(
                    BeskedfordelerSender.ReplyQueue, true, It.IsAny<string>(), false, false,
                    It.IsAny<IDictionary<string, object?>>(), It.IsAny<IAsyncBasicConsumer>(),
                    timeout.Token))
                .Callback<string, bool, string, bool, bool, IDictionary<string, object?>, IAsyncBasicConsumer, CancellationToken>(
                    (_, _, _, _, _, _, value, _) => consumer = value)
                .ReturnsAsync("test-consumer");
            channel.Setup(x => x.BasicPublishAsync(
                    BeskedfordelerSender.Exchange, "", true, It.IsAny<BasicProperties>(),
                    It.IsAny<ReadOnlyMemory<byte>>(), timeout.Token))
                .Returns((string _, string _, bool _, BasicProperties properties, ReadOnlyMemory<byte> _, CancellationToken ct) =>
                    new ValueTask(consumer!.HandleBasicDeliverAsync(
                        "test-consumer", 1, false, "", "",
                        new BasicProperties { CorrelationId = properties.CorrelationId },
                        Encoding.UTF8.GetBytes(reply), ct)));

            var result = await BeskedfordelerSender.Publish(channel.Object, "test-token", "<test />", timeout.Token);

            Assert.Equal(reply, result);
        }
    }
}
