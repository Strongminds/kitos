using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.DomainModel;
using Presentation.Web.Models.API.V2.Internal.Request;
using Presentation.Web.Models.API.V2.Internal.Response;
using Tests.Integration.Presentation.Web.Tools;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Integration.Presentation.Web.HelpTexts
{
    public class HelpTextsInternalApiV2Test: WithAutoFixture
    {
        [Fact]
        public async Task Can_Get_HelpTexts()
        {
            var expected = new HelpText()
            {
                Description = A<string>(),
                Title = A<string>(),
                Key = A<string>()
            };
            DatabaseAccess.MutateEntitySet<HelpText>(repo => repo.Insert(expected));

            var response = await HelpTextsInternalV2Helper.Get();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.ReadResponseBodyAsAsync<IEnumerable<HelpTextResponseDTO>>();
            var actual = content.First(ht => ht.Key == expected.Key);
            Assert.NotNull(actual);
            Assert.Equal(expected.Key, actual.Key);
            Assert.Equal(expected.Description, actual.Description);
            Assert.Equal(expected.Title, actual.Title);
        }

        [Fact]
        public async Task Can_Create_HelpText()
        {
            var dto = A<HelpTextCreateRequestDTO>();

            var response = await HelpTextsInternalV2Helper.Create(dto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var actual = await response.ReadResponseBodyAsAsync<HelpTextResponseDTO>();
            Assert.NotNull(actual);
            Assert.Equal(dto.Key, actual.Key);
            Assert.Equal(dto.Description, actual.Description);
            Assert.Equal(dto.Title, actual.Title);
        }

        [Fact]
        public async Task Can_Delete_HelpText()
        {
            var dto = A<HelpTextCreateRequestDTO>();
            var createResponse = await HelpTextsInternalV2Helper.Create(dto);
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

            var response = await HelpTextsInternalV2Helper.Delete(dto.Key);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var allHelpTextsResponse = await HelpTextsInternalV2Helper.Get();
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
            var allHelpTexts = await allHelpTextsResponse.ReadResponseBodyAsAsync<IEnumerable<HelpText>>();
            Assert.False(allHelpTexts.Any(ht => ht.Key == dto.Key));
        }
    }
}
