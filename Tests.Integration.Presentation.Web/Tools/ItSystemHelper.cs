using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V1;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools
{
    public class ItSystemHelper
    {
        public static async Task<ItSystemDTO> CreateItSystemInOrganizationAsync(string itSystemName, int orgId, AccessModifier accessModifier)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var itSystem = new
            {
                name = itSystemName,
                belongsToId = orgId,
                organizationId = orgId,
                AccessModifier = accessModifier
            };

            using var createdResponse = await HttpApi.PostWithCookieAsync(TestEnvironment.CreateUrl("api/itsystem"), cookie, itSystem);
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            var response = await createdResponse.ReadResponseBodyAsKitosApiResponseAsync<ItSystemDTO>();

            Assert.Equal(orgId, response.OrganizationId);
            Assert.Equal(orgId, response.BelongsToId);
            Assert.Equal(itSystemName, response.Name);
            return response;
        }

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
        
        public static async Task DeleteItSystemAsync(int systemId, int organizationId, Cookie optionalLogin = null)
        {
            using var response = await SendDeleteItSystemAsync(systemId, organizationId, optionalLogin);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        public static async Task<HttpResponseMessage> SendDeleteItSystemAsync(int systemId, int organizationId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var url = TestEnvironment.CreateUrl($"api/itsystem/{systemId}?organizationId={organizationId}");

            return await HttpApi.DeleteWithCookieAsync(url, cookie);
        }

        public static async Task<ItSystemDTO> SetNameAsync(
            int systemId,
            string name,
            int organizationId,
            Cookie optionalLogin = null)
        {
            using var response = await SendSetNameRequestAsync(systemId, name, organizationId, optionalLogin);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadResponseBodyAsKitosApiResponseAsync<ItSystemDTO>();
        }

        public static async Task<HttpResponseMessage> SendSetNameRequestAsync(
            int systemId,
            string name,
            int organizationId,
            Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var url = TestEnvironment.CreateUrl($"api/itsystem/{systemId}?organizationId={organizationId}");
            var body = new
            {
                name = name
            };

            return await HttpApi.PatchWithCookieAsync(url, cookie, body);
        }

        public static async Task<HttpResponseMessage> SendGetSystemRequestAsync(int systemId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var url = TestEnvironment.CreateUrl($"api/itsystem/{systemId}");

            return await HttpApi.GetWithCookieAsync(url, cookie);
        }

        public static async Task<ItSystemDTO> GetSystemAsync(int systemId, Cookie optionalLogin = null)
        {
            using var response = await SendGetSystemRequestAsync(systemId, optionalLogin);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadResponseBodyAsKitosApiResponseAsync<ItSystemDTO>();
        }

        public static async Task<HttpResponseMessage> SendSetBusinessTypeRequestAsync(
            int systemId,
            int businessTypeId,
            int organizationId,
            Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var url = TestEnvironment.CreateUrl($"api/itsystem/{systemId}?organizationId={organizationId}");
            var body = new
            {
                businessTypeId = businessTypeId
            };

            return await HttpApi.PatchWithCookieAsync(url, cookie, body);
        }

        public static async Task<HttpResponseMessage> SendSetBelongsToRequestAsync(
            int systemId,
            int? belongsToId,
            int organizationId,
            Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var url = TestEnvironment.CreateUrl($"api/itsystem/{systemId}?organizationId={organizationId}");
            var body = new
            {
                belongsToId = belongsToId
            };
            return await HttpApi.PatchWithCookieAsync(url, cookie, body);
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
