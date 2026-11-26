using Kitos.Integration.Contracts;
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
            var user = Guid.NewGuid();
            const string cvr = "12345678";
            var reader = new BeskedfordelerXmlReader(BeskedfordelerXmlReader.LoadSchemas());
            var ids = new HashSet<Guid>();
            var payloads = new HashSet<string>();
            var before = DateTimeOffset.UtcNow;
            var messages = Enumerable.Range(0, 10).Select(_ => TestMessage.Create(sender, cvr, user)).ToArray();
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
                Assert.Equal("94ef3c82-10e9-4d12-a9d9-7a7242d5ba83", Value("Beskedtype"));
                Assert.Equal(user.ToString("D"), Value("ObjektId"));
                Assert.Equal("2b808769-8d09-4aa3-ac28-f8ac3526010f", Value("ObjektType"));
                Assert.Equal("9edb0c34-36e2-4d87-aba8-3ca7ade718ee", Value("ObjektHandling"));
                Assert.Equal($"urn:oio:cvr-nr:{cvr}", Value("TilladtModtager"));
                Assert.Equal($"urn:oio:cvr-nr:{cvr}", Value("ObjektAnsvarligMyndighed"));
                var payload = message.Element(TestMessage.Envelope + "Beskeddata")!.Value;
                Assert.True(payloads.Add(payload));
                var audit = XElement.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
                Assert.Equal(messageId.ToString("D"), audit.Element("TransactionID")!.Value);
                Assert.Equal(sender.ToString("D"), audit.Element("Bruger")!.Value);
                Assert.Equal("BrugerNedlagt", audit.Element("Operation")!.Value);
                foreach (var time in message.Descendants(TestMessage.Sagdok + "TidsstempelDatoTid"))
                    Assert.InRange(DateTimeOffset.Parse(time.Value), before, after);
                var input = new XElement(BeskedfordelerMessage.Sts + "ModtagBeskedInput", message);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input.ToString()));
                await reader.Read(stream, CancellationToken.None);
            }
        }

        [Fact]
        public void Create_EmptyAffectedUser_RejectsMessage()
        {
            Assert.Throws<ArgumentException>(() => TestMessage.Create(Guid.NewGuid(), "12345678", Guid.Empty));
        }

        [Theory]
        [InlineData("")]
        [InlineData("1234567")]
        [InlineData("1234567a")]
        public void Create_InvalidMunicipality_RejectsMessage(string cvr)
        {
            Assert.Throws<ArgumentException>(() => TestMessage.Create(Guid.NewGuid(), cvr, Guid.NewGuid()));
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
