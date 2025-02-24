using System.Threading.Tasks;
using Core.DomainModel;
using Core.DomainModel.Organization;
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
            var token = await HttpApi.GetTokenAsync(OrganizationRole.GlobalAdmin);
            var org = await CreateOrganizationAsync();
            var (systemUuid, _) = await CreateSystemAsync(org.Id, AccessModifier.Public);
            var request = new UpdateDBSPropertiesRequestDTO { SystemName = A<string>() };

            var response = await DBSV2Helper.PatchSystem(systemUuid, request);

            Assert.True(response.IsSuccessStatusCode);
            var systemAfter = await ItSystemV2Helper.GetSingleAsync(token.Token, systemUuid);
            Assert.Equal(request.SystemName, systemAfter.DBSName);
        } 
    }
}
