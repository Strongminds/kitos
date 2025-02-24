using System;
using System.Threading.Tasks;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V1;
using Presentation.Web.Models.API.V2.Request.System.Regular;
using Tests.Integration.Presentation.Web.Tools;
using Tests.Integration.Presentation.Web.Tools.External;
using Xunit;

namespace Tests.Integration.Presentation.Web.ItSystem.V2
{
    public class ItSystemDBSV2ApiTest : BaseItSystemsApiV2Test
    {

        [Fact]
        public async Task Can_Update_DBS_Name()
        {
            var (token, systemUuid, _) = await CreatePrerequisites();
            var request = new UpdateDBSPropertiesRequestDTO { SystemName = A<string>() };

            var response = await DBSV2Helper.PatchSystem(systemUuid, request);

            Assert.True(response.IsSuccessStatusCode);
            var systemAfter = await ItSystemV2Helper.GetSingleAsync(token, systemUuid);
            Assert.Equal(request.SystemName, systemAfter.DBSName);
        }

        [Fact]
        public async Task Can_Update_DBS_Data_Processor_Name()
        {

            var (token, systemUuid, _) = await CreatePrerequisites();
            var request = new UpdateDBSPropertiesRequestDTO { DataProcessorName = A<string>() };

            var response = await DBSV2Helper.PatchSystem(systemUuid, request);

            Assert.True(response.IsSuccessStatusCode);
            var systemAfter = await ItSystemV2Helper.GetSingleAsync(token, systemUuid);
            Assert.Equal(request.DataProcessorName, systemAfter.DBSDataProcessorName);
        }

        [Fact]
        public async Task API_User_Can_Perform_DBS_Update()
        {
            var (_, systemUuid, orgId) = await CreatePrerequisites();
            var (_, _, apiToken) = await HttpApi.CreateUserAndGetToken(CreateEmail(), OrganizationRole.User, orgId, true, false);
            var request = A<UpdateDBSPropertiesRequestDTO>();

            var response = await DBSV2Helper.PatchSystem(systemUuid, request, apiToken);

            Assert.True(response.IsSuccessStatusCode);
        }

        private async Task<(string, Guid, int)> CreatePrerequisites()
        {
            var token = await HttpApi.GetTokenAsync(OrganizationRole.GlobalAdmin);
            var org = await CreateOrganizationAsync();
            var (systemUuid, _) = await CreateSystemAsync(org.Id, AccessModifier.Public);
            return (token.Token, systemUuid, org.Id);
        }
    }
}
