using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AutoFixture;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Core.DomainServices.Extensions;
using Presentation.Web.Models.API.V1;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools
{
    public static class OrganizationHelper
    {
        public static async Task<OrganizationDTO> GetOrganizationAsync(int organizationId, Cookie optionalCookie = null)
        {
            using var response = await SendGetOrganizationRequestAsync(organizationId, optionalCookie);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadResponseBodyAsKitosApiResponseAsync<OrganizationDTO>();
        }

        public static async Task<HttpResponseMessage> SendGetOrganizationRequestAsync(int organizationId, Cookie optionalCookie = null)
        {
            var cookie = optionalCookie ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl($"api/organization/{organizationId}");
            return await HttpApi.GetWithCookieAsync(url, cookie);
        }

        public static async Task<OrganizationDTO> CreateOrganizationAsync(int owningOrganizationId, string name, string cvr, OrganizationTypeKeys type, AccessModifier accessModifier, Cookie optionalLogin = null)
        {
            using var createdResponse = await SendCreateOrganizationRequestAsync(owningOrganizationId, name, cvr, type, accessModifier, optionalLogin);
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            return await createdResponse.ReadResponseBodyAsKitosApiResponseAsync<OrganizationDTO>();
        }

        public static async Task<HttpResponseMessage> SendCreateOrganizationRequestAsync(int owningOrganizationId, string name, string cvr, OrganizationTypeKeys type, AccessModifier accessModifier, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl("api/Organization?organizationId=0");

            var body = new OrganizationDTO
            {
                AccessModifier = accessModifier,
                Cvr = cvr,
                Name = name,
                TypeId = (int)type
            };

            return await HttpApi.PostWithCookieAsync(url, cookie, body);
        }

        public static async Task<OrgUnitDTO> CreateOrganizationUnitAsync(int organizationId, string orgUnitName, int? parentId = null, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl("api/organizationUnit/");
            var fixture = new Fixture();
            var parentUnitId =
                parentId ??
                //Fallback to the root of the organization
                DatabaseAccess.MapFromEntitySet<OrganizationUnit, int>(units => units.AsQueryable().ByOrganizationId(organizationId).First(x => x.ParentId == null).Id);
            var body = new
            {
                name = orgUnitName,
                organizationId = organizationId,
                parentId = parentUnitId,
                ean = fixture.Create<uint>() % 10000,
                localId = fixture.Create<string>()
            };

            using var createdResponse = await HttpApi.PostWithCookieAsync(url, cookie, body);
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            return await createdResponse.ReadResponseBodyAsKitosApiResponseAsync<OrgUnitDTO>();
        }
    }
}
