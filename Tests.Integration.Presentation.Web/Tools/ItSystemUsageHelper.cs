using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel.ItSystemUsage.Read;
using Core.DomainModel.Organization;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools
{
    public class ItSystemUsageHelper
    {
        public static async Task<IEnumerable<ItSystemUsageOverviewReadModel>> QueryReadModelByNameContent(int organizationId, string nameContent, int top, int skip, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl($"odata/Organizations({organizationId})/ItSystemUsageOverviewReadModels?$expand=RoleAssignments,ItSystemTaskRefs,SensitiveDataLevels,ArchivePeriods,DataProcessingRegistrations,DependsOnInterfaces,IncomingRelatedItSystemUsages,OutgoingRelatedItSystemUsages,RelevantOrganizationUnits,AssociatedContracts&$filter=contains(SystemName,'{nameContent}')&$top={top}&$skip={skip}&$orderBy=SystemName"), cookie);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadOdataListResponseBodyAsAsync<ItSystemUsageOverviewReadModel>();
        }

        public static async Task<HttpResponseMessage> SendOdataDeleteRightRequestAsync(int rightId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            return await HttpApi.DeleteWithCookieAsync(TestEnvironment.CreateUrl($"odata/ItSystemRights({rightId})"), cookie);
        }
    }
}