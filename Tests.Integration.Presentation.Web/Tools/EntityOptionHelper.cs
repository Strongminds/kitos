using System.Net;
using System.Threading.Tasks;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V1;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools
{
    public static class EntityOptionHelper
    {
        public static class ResourceNames
        {
            public const string BusinessType = "BusinessTypes";
        }

        public static async Task<OptionDTO> CreateOptionTypeAsync(string resource, string optionName, int organizationId, Cookie optionalLogin = null, string description = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl($"odata/{resource}?organizationId={organizationId}");

            var body = new
            {
                IsObligatory = true,
                IsEnabled = true,
                Name = optionName,
                Description = description ?? ""
            };

            using var response = await HttpApi.PostWithCookieAsync(url, cookie, body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.ReadResponseBodyAsAsync<OptionDTO>();
        }
    }
}
