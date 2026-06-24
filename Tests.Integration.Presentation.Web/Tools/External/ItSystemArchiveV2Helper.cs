using Presentation.Web.Models.API.V2.Request.SystemUsage;
using Presentation.Web.Models.API.V2.Response.SystemUsage;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools.External
{
    public static class ItSystemArchiveV2Helper
    {
        private const string BasePath = "api/v2/it-system-archives";

        public static async Task<ItSystemArchiveResponseDTO> GetArchiveAsync(string token, Guid archiveUuid)
        {
            using var response = await SendGetArchiveAsync(token, archiveUuid);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadResponseBodyAsAsync<ItSystemArchiveResponseDTO>();
        }

        public static async Task<HttpResponseMessage> SendGetArchiveAsync(string token, Guid archiveUuid)
        {
            var url = TestEnvironment.CreateUrl($"{BasePath}/{archiveUuid}");
            return await HttpApi.GetWithTokenAsync(url, token);
        }

        public static async Task DeleteArchiveAsync(string token, Guid archiveUuid)
        {
            using var response = await SendDeleteArchiveAsync(token, archiveUuid);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public static async Task<HttpResponseMessage> SendDeleteArchiveAsync(string token, Guid archiveUuid)
        {
            var url = TestEnvironment.CreateUrl($"{BasePath}/{archiveUuid}");
            return await HttpApi.DeleteWithTokenAsync(url, token);
        }
    }
}
