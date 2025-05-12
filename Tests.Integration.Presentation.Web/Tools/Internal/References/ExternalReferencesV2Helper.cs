using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V2.Request.Generic.ExternalReferences;
using Presentation.Web.Models.API.V2.Response.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools.Internal.References
{
    public class ExternalReferencesV2Helper
    {
        private const string ItSystemPrefix = "api/v2/it-systems";

        public static async Task<ExternalReferenceDataResponseDTO> PostItSystemReference(
            Guid systemUuid, ExternalReferenceDataWriteRequestDTO request)
        {
            var token = await HttpApi.GetTokenAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl(GetItSystemBaseUrl(systemUuid));
            using var response = await HttpApi.PostWithTokenAsync(url, request, token.Token);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.ReadResponseBodyAsAsync<ExternalReferenceDataResponseDTO>();
        }

        public static async Task<HttpResponseMessage> DeleteItSystemReferenceAsync(Guid systemUuid,
            Guid referenceUuid)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl($"{GetItSystemBaseUrl(systemUuid)}/{referenceUuid}");
            return await HttpApi.DeleteWithCookieAsync(url, cookie);
        }

        private static string GetItSystemBaseUrl(Guid systemUuid)
        {
            return $"{ItSystemPrefix}/{systemUuid}/external-references";
        }
    }
}
