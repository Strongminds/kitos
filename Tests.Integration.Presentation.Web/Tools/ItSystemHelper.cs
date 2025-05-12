using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V1;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools
{
    public class ItSystemHelper
    {
        public static async Task<ItSystemUsageDTO> TakeIntoUseAsync(int itSystemId, int orgId)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var itSystem = new
            {
                itSystemId = itSystemId,
                organizationId = orgId,
            };

            using var createdResponse = await HttpApi.PostWithCookieAsync(TestEnvironment.CreateUrl("api/itSystemUsage"), cookie, itSystem);
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            var response = await createdResponse.ReadResponseBodyAsKitosApiResponseAsync<ItSystemUsageDTO>();

            Assert.Equal(orgId, response.OrganizationId);
            Assert.Equal(itSystemId, response.ItSystemId);
            return response;
        }

        public static async Task<HttpResponseMessage> SendAddTaskRefRequestAsync(int systemId, int taskRefId, int organizationId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var url = TestEnvironment.CreateUrl($"api/itSystem/{systemId}?taskId={taskRefId}&organizationId={organizationId}");
            var body = new
            {
            };
            return await HttpApi.PostWithCookieAsync(url, cookie, body);
        }
    }
}
