using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V1;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools
{
    public static class InterfaceHelper
    {
        public static ItInterfaceDTO CreateInterfaceDto(
            string name,
            string interfaceId,
            int orgId,
            AccessModifier access,
            string notes = null)
        {
            return new ItInterfaceDTO
            {
                ItInterfaceId = interfaceId,
                Name = name,
                OrganizationId = orgId,
                AccessModifier = access,
                Note = notes
            };
        }

        public static async Task<HttpResponseMessage> SendDeleteInterfaceRequestAsync(int id)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl($"api/itinterface/{id}?{KitosApiConstants.UnusedOrganizationIdParameter}"); //Org id not used

            return await HttpApi.DeleteWithCookieAsync(url, cookie);
        }

        public static async Task<ItInterfaceDTO> CreateInterface(ItInterfaceDTO input)
        {
            using var createdResponse = await SendCreateInterface(input);
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            return await createdResponse.ReadResponseBodyAsKitosApiResponseAsync<ItInterfaceDTO>();
        }

        public static async Task<HttpResponseMessage> SendCreateInterface(ItInterfaceDTO input, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl("api/itinterface");

            return await HttpApi.PostWithCookieAsync(url, cookie, input);
        }

        public static async Task<ItInterfaceDTO> SetUrlAsync(int interfaceId, string url, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            using (var response = await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/itinterface/{interfaceId}?{KitosApiConstants.UnusedOrganizationIdParameter}"), cookie, new { url = url }))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                return await response.ReadResponseBodyAsKitosApiResponseAsync<ItInterfaceDTO>();
            }
        }

        public static async Task DeleteInterfaceAsync(int itInterfaceId)
        {
            using var response = await SendDeleteInterfaceRequestAsync(itInterfaceId);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
