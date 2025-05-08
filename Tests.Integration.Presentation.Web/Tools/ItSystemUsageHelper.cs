using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel.Organization;

namespace Tests.Integration.Presentation.Web.Tools
{
    public class ItSystemUsageHelper
    {
        public static async Task<HttpResponseMessage> SendOdataDeleteRightRequestAsync(int rightId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            return await HttpApi.DeleteWithCookieAsync(TestEnvironment.CreateUrl($"odata/ItSystemRights({rightId})"), cookie);
        }
    }
}