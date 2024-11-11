using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.DomainModel;
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
    }
}
