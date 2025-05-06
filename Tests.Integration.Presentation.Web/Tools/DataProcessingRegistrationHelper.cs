using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel.GDPR.Read;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V1;
using Presentation.Web.Models.API.V1.GDPR;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools
{
    public static class DataProcessingRegistrationHelper
    {
        public static async Task<IEnumerable<DataProcessingRegistrationReadModel>> QueryReadModelByNameContent(int organizationId, string nameContent, int top, int skip, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl($"odata/Organizations({organizationId})/DataProcessingRegistrationReadModels?$expand=RoleAssignments&$filter=contains(Name,'{nameContent}')&$top={top}&$skip={skip}&$orderBy=Name"), cookie);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadOdataListResponseBodyAsAsync<DataProcessingRegistrationReadModel>();
        }

        public static async Task<IEnumerable<BusinessRoleDTO>> GetAvailableRolesAsync(int id, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}/available-roles"), cookie);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadResponseBodyAsKitosApiResponseAsync<IEnumerable<BusinessRoleDTO>>();
        }
        
        public static async Task<IEnumerable<UserWithEmailDTO>> GetAvailableUsersAsync(int id, int roleId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}/available-roles/{roleId}/applicable-users"), cookie);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadResponseBodyAsKitosApiResponseAsync<IEnumerable<UserWithEmailDTO>>();
        }

        public static async Task<HttpResponseMessage> SendAssignRoleRequestAsync(int id, int roleId, int userId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            return await HttpApi.PatchWithCookieAsync(
                TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}/roles/assign"), cookie,
                new AssignRoleDTO
                {
                    RoleId = roleId,
                    UserId = userId
                });
        }

        public static async Task<HttpResponseMessage> SendRemoveRoleRequestAsync(int id, int roleId, int userId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            return await HttpApi.PatchWithCookieAsync(
                TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}/roles/remove/{roleId}/from/{userId}"), cookie,
                new { });
        }
    }
}
