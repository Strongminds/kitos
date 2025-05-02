using Core.DomainModel.Organization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tests.Integration.Presentation.Web.Tools
{
    public static class UserHelper
    {
        public static async Task<HttpResponseMessage> SendDeleteUserAsync(int userId, Cookie optionalLogin = null, int organizationId = 0)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var request = $"api/v1/user/delete/{userId}";
            if (organizationId != 0)
            {
                return await HttpApi.DeleteWithCookieAsync(TestEnvironment.CreateUrl(request + $"/{organizationId}"), cookie);
            }
            return await HttpApi.DeleteWithCookieAsync(TestEnvironment.CreateUrl(request), cookie);
        }
    }
}
