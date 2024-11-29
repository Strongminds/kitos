using Core.DomainModel.Organization;
using Core.DomainModel;
using Presentation.Web.Models.API.V1;
using System.Threading.Tasks;
using Tests.Integration.Presentation.Web.Tools;
using Tests.Toolkit.Patterns;
using Xunit;
using Tests.Integration.Presentation.Web.Tools.Internal;
using System.Collections.Generic;
using Presentation.Web.Models.API.V2.Internal.Response;
using System;
using Tests.Integration.Presentation.Web.Tools.External;
using Presentation.Web.Models.API.V2.Request.System.Regular;
using Presentation.Web.Models.API.V2.Request.SystemUsage;
using Presentation.Web.Models.API.V2.Response.SystemUsage;

namespace Tests.Integration.Presentation.Web.Internal.GdprReport
{
    public class GdprReportApiV2Test : WithAutoFixture
    {

        [Fact]
        public async Task Can_Get_Gdpr_Report()
        {
            var org = await CreateOrganizationAsync();
            await CreateUsage(org.Uuid);

            var response = await GdprReportV2Helper.GetGdprReportAsync(org.Uuid);

            Assert.True(response.IsSuccessStatusCode);
            var body = await response.ReadResponseBodyAsAsync<IEnumerable<GdprReportResponseDTO>>();
            Assert.NotEmpty(body);
        }

        private async Task CreateUsage(Guid organizationUuid)
        {
            var token = await HttpApi.GetTokenAsync(OrganizationRole.GlobalAdmin);

            var systemRequest = new CreateItSystemRequestDTO { OrganizationUuid = organizationUuid, Name = A<string>()};
            systemRequest.OrganizationUuid = organizationUuid;
            var response = await ItSystemV2Helper.SendCreateSystemAsync(token.Token, systemRequest);
            Assert.True(response.IsSuccessStatusCode);
            var system = await response.ReadResponseBodyAsAsync<ItSystemUsageResponseDTO>();

            var usageRequest = new CreateItSystemUsageRequestDTO { SystemUuid = system.Uuid, OrganizationUuid = organizationUuid};
            usageRequest.OrganizationUuid = organizationUuid;
            usageRequest.SystemUuid = system.Uuid;
            var usageResponse = await ItSystemUsageV2Helper.SendPostAsync(token.Token, usageRequest);

            Assert.True(usageResponse.IsSuccessStatusCode);
        } 

        private async Task<OrganizationDTO> CreateOrganizationAsync()
        {
            var organization = await OrganizationHelper.CreateOrganizationAsync(TestEnvironment.DefaultOrganizationId, A<string>(),
                "11223344", OrganizationTypeKeys.Kommune, AccessModifier.Local);
            Assert.NotNull(organization);
            return organization;
        }
    }
}
