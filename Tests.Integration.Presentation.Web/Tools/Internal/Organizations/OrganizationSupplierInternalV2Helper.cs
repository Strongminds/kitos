using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V2.Response.Organization;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools.Internal.Organizations
{
    public class OrganizationSupplierInternalV2Helper
    {
        private const string ApiPrefix = "api/v2/internal/organizations";
        
        public static async Task<ShallowOrganizationResponseDTO> AddSupplier(Guid organizationUuid, Guid supplierUuid)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.PostWithCookieAsync(TestEnvironment.CreateUrl($"{ApiPrefix}/{organizationUuid}/suppliers/{supplierUuid}"), cookie, null);
            var res = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadResponseBodyAsAsync<ShallowOrganizationResponseDTO>();
        }

        public static async Task<IEnumerable<ShallowOrganizationResponseDTO>> GetSuppliers(Guid organizationUuid)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl($"{ApiPrefix}/{organizationUuid}/suppliers"), cookie);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadResponseBodyAsAsync<IEnumerable<ShallowOrganizationResponseDTO>>();
        }
    }
}
