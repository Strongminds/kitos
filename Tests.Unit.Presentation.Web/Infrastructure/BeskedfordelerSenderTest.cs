using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Infrastructure.STS.Beskedfordeler;
using Moq;
using RabbitMQ.Client;
using Tools.Beskedfordeler;
using Xunit;

namespace Tests.Unit.Presentation.Web.Infrastructure
{
    public class BeskedfordelerSenderTest
    {
        [Fact]
        public async Task Create_RepeatedCalls_ProduceDistinctSchemaValidMessagesWithStableRouting()
        {
            var sender = Guid.NewGuid();
            const string cvr = "12345678";
            var reader = new BeskedfordelerXmlReader(BeskedfordelerXmlReader.LoadSchemas());
            var ids = new HashSet<Guid>();
            var payloads = new HashSet<string>();
            var before = DateTimeOffset.UtcNow;
            var messages = Enumerable.Range(0, 10).Select(_ => TestMessage.Create(sender, cvr)).ToArray();
            var after = DateTimeOffset.UtcNow;

            foreach (var message in messages)
            {
                string Value(string name) => message.Descendants(TestMessage.Envelope + name).Single().Elements().Single().Value;
                var messageId = Guid.Parse(Value("BeskedId"));
                Assert.NotEqual(Guid.Empty, messageId);
                Assert.True(ids.Add(messageId));
                Assert.True(ids.Add(Guid.Parse(Value("ObjektRegistreringId"))));
                Assert.Equal(sender.ToString("D"), Value("BeskedAnsvarligAktoer"));
                Assert.Equal(sender.ToString("D"), Value("RegistreringsAktoer"));
                Assert.Equal(TestMessage.MessageType.ToString("D"), Value("Beskedtype"));
                Assert.Equal($"urn:oio:cvr-nr:{cvr}", Value("TilladtModtager"));
                Assert.Equal($"urn:oio:cvr-nr:{cvr}", Value("ObjektAnsvarligMyndighed"));
                var payload = message.Element(TestMessage.Envelope + "Beskeddata")!.Value;
                Assert.True(payloads.Add(payload));
                Assert.Contains(messageId.ToString("D"), payload);
                foreach (var time in message.Descendants(TestMessage.Sagdok + "TidsstempelDatoTid"))
                    Assert.InRange(DateTimeOffset.Parse(time.Value), before, after);
                var input = new XElement(BeskedfordelerResponse.Sts + "ModtagBeskedInput", message);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input.ToString()));
                await reader.Read(stream, CancellationToken.None);
            }
        }

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
