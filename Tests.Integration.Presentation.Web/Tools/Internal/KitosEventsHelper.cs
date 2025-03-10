using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel.Organization;

namespace Tests.Integration.Presentation.Web.Tools.Internal
{
    public class KitosEventsHelper
    {
        public static async Task<HttpResponseMessage> PublishEvent()
        {
            var token = await HttpApi.GetTokenAsync(OrganizationRole.GlobalAdmin);
            var body = new { };
            var url = "localhost:7226/api/publish";
            return await HttpApi.PostWithTokenAsync(url, body, token.Token);
        }
    }
}
