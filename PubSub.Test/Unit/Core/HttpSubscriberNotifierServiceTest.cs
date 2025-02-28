using PubSub.Core.Services.Notifier;
using Moq;
using AutoFixture;
using Moq.Protected;
using System.Net;
using PubSub.Test.Base.Tests.Toolkit.Patterns;

namespace PubSub.Test.Unit.Core
{
    public class HttpSubscriberNotifierServiceTest: WithAutoFixture
    {
        [Fact]
        public async Task Can_Post_With_Client_From_Factory()
        {  
            HttpRequestMessage? capturedRequest = null;
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
                {
                    capturedRequest = req;
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                });
            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = A<Uri>()};
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock
                .Setup(factory => factory.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
            var sut = new HttpSubscriberNotifierService(httpClientFactoryMock.Object);
            var message = A<string>();
            var callback = A<string>();

            await sut.Notify(message, callback);

            httpClientFactoryMock.Verify(_ => _.CreateClient(It.IsAny<string>()), Times.Once);
            var content = await capturedRequest.Content.ReadAsStringAsync();
            Assert.Contains(message, content);
        }
    }
}
