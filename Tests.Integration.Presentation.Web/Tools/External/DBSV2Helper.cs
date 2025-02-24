using System;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V2.Request.System.Regular;

namespace Tests.Integration.Presentation.Web.Tools.External
{
    public class DBSV2Helper
    {
        private const string ControllerRoutePrefix = "api/v2/it-systems";

        public static async Task<HttpResponseMessage> PatchSystem(Guid systemUuid,
            UpdateDBSPropertiesRequestDTO request)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = GetUrl(systemUuid);
            return await HttpApi.PatchWithCookieAsync(url, cookie, request);
        }

        private static Uri GetUrl(Guid systemUuid)
        {
            return TestEnvironment.CreateUrl($"{ControllerRoutePrefix}/{systemUuid}/dbs");
        }
    }
}
