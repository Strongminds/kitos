using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Tests.Toolkit.Patterns;
using Core.ApplicationServices.Authentication;
using Core.ApplicationServices.Model.KitosEvents;
using Core.ApplicationServices.KitosEvents;
using Infrastructure.Services.Http;
using Core.ApplicationServices.Model.Authentication;
using Core.DomainModel;
using Serilog;

namespace Tests.Unit.Core.ApplicationServices.KitosEvents
{
    public class HttpEventPublisherTest : WithAutoFixture
    {
        private readonly Mock<IKitosHttpClient> _httpClientMock;
        private readonly Mock<ILogger> _logger;
        private readonly Mock<IKitosInternalTokenIssuer> _tokenIssuerMock;
        private readonly HttpEventPublisher _sut;
        private readonly string _pubSubBaseUrl = "http://localhost/";

        public HttpEventPublisherTest()
        {
            _httpClientMock = new Mock<IKitosHttpClient>();
            _tokenIssuerMock = new Mock<IKitosInternalTokenIssuer>();
            _logger = new Mock<ILogger>();
            _sut = new HttpEventPublisher(_httpClientMock.Object, _tokenIssuerMock.Object, _pubSubBaseUrl, _logger.Object);
        }

        [Fact]
        public async Task Can_Post_Event()
        {
            var eventDTO = A<KitosEventDTO>();
            var token = A<string>();
            var kitosToken = new KitosApiToken(new User(), token, A<DateTime>());
            _tokenIssuerMock.Setup(t => t.GetToken()).Returns(kitosToken);

            var expectedUrl = new Uri(new Uri(_pubSubBaseUrl), "api/publish");

            await _sut.PostEventAsync(eventDTO);

            _httpClientMock.Verify(x => x.PostAsync(eventDTO, expectedUrl, kitosToken.Value), Times.Once);
        }
    }
}
