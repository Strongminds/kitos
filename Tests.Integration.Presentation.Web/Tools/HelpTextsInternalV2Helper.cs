using Core.DomainModel.Organization;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tests.Integration.Presentation.Web.Tools
{
    public class HelpTextsInternalV2Helper
    {
        private const string Endpoint = "api/v2/internal/help-texts";

        public static async Task<HttpResponseMessage> Get()
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            return await HttpApi.GetWithCookieAsync(
                TestEnvironment.CreateUrl(Endpoint), cookie);
        }
    }
}
