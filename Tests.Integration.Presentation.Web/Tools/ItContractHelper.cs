using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V1;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools
{
    public static class ItContractHelper
    {
        public static async Task<ItContractDTO> CreateContract(string name, int organizationId)
        {
            using var createdResponse = await SendCreateContract(name, organizationId);
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            var response = await createdResponse.ReadResponseBodyAsKitosApiResponseAsync<ItContractDTO>();

            Assert.Equal(organizationId, response.OrganizationId);
            Assert.Equal(name, response.Name);
            return response;
        }

        public static async Task<HttpResponseMessage> SendCreateContract(string name, int organizationId)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var body = new
            {
                name = name,
                organizationId = organizationId,
            };

            return await HttpApi.PostWithCookieAsync(TestEnvironment.CreateUrl($"api/itcontract?organizationId={organizationId}"), cookie, body);
        }

        public static async Task<ItContractDTO> GetItContract(int contractId)
        {
            using var result = await SendGetItContract(contractId);
            var contract = await result.ReadResponseBodyAsKitosApiResponseAsync<ItContractDTO>();

            return contract;
        }

        public static async Task<HttpResponseMessage> SendGetItContract(int contractId)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl($"api/ItContract/{contractId}");

            return await HttpApi.GetWithCookieAsync(url, cookie);
        }

        public static async Task<ItSystemUsageSimpleDTO> AddItSystemUsage(int contractId, int usageId, int organizationId)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var body = new
            {
                systemUsageId = usageId,
                organizationId = organizationId,
            };

            using var createdResponse = await HttpApi.PostWithCookieAsync(TestEnvironment.CreateUrl($"api/itcontract/{contractId}?systemUsageId={usageId}&organizationId={organizationId}"), cookie, body);
            Assert.Equal(HttpStatusCode.OK, createdResponse.StatusCode);
            var response = await createdResponse.ReadResponseBodyAsKitosApiResponseAsync<IEnumerable<ItSystemUsageSimpleDTO>>();

            var addedMapping = response.FirstOrDefault(x => x.Id == usageId);
            Assert.NotNull(addedMapping);
            return addedMapping;
        }

        public static async Task<ItContractDTO> PatchContract(int contractId, int orgId, object body)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            using var okResponse = await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"/api/itcontract/{contractId}?organizationId={orgId}"), cookie, body);
            Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
            return await okResponse.ReadResponseBodyAsKitosApiResponseAsync<ItContractDTO>();
        }

        public static async Task<HttpResponseMessage> SendAssignDataProcessingRegistrationAsync(int contractId, int registrationId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            return await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/itcontract/{contractId}/data-processing-registration/assign"), cookie, new { Value = registrationId });
        }

        public static async Task<HttpResponseMessage> SendAssignResponsibleOrgUnitAsync(int contractId, int orgUnitId, int organizationId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            return await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/itcontract/{contractId}?organizationId={organizationId}"), cookie, new { responsibleOrganizationUnitId = orgUnitId });
        }

        public static async Task<HttpResponseMessage> SendAssignSupplierAsync(int contractId, int supplierId, int organizationId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            return await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/itcontract/{contractId}?organizationId={organizationId}"), cookie, new { supplierId = supplierId });
        }
    }
}