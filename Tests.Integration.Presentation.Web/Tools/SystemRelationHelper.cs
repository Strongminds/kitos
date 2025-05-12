using System.Net;
using System.Threading.Tasks;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V1.SystemRelations;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools
{
    public class SystemRelationHelper
    {
        public static async Task<SystemRelationDTO> PostRelationAsync(CreateSystemRelationDTO input, Cookie login = null)
        {
            login ??= await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl("api/v1/systemrelations");

            using (var response = await HttpApi.PostWithCookieAsync(url, login, input))
            {
                Assert.Equal(HttpStatusCode.Created,response.StatusCode);
                return await response.ReadResponseBodyAsKitosApiResponseAsync<SystemRelationDTO>();
            }
        }
    }
}
