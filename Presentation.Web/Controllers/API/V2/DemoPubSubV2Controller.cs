using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Threading.Tasks;
using Presentation.Web.Controllers.API.V2;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security.Cryptography;
using Presentation.Web.Models.API.V2.Response.Options;
using Swashbuckle.Swagger.Annotations;
using System.Net;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Controllers.API.V2.Common;
using Presentation.Web.Controllers.API.V2.External;
using Presentation.Web.Controllers.API.V2.Internal;


namespace Presentation.Web.Controllers.API.V2
{
    [RoutePrefix("api/v2/pubsub")]
    [AllowAnonymous]
    public class DemoPubSubV2Controller() : InternalApiV2Controller
    {
        //Select an api url here depending on if you are connecting to a local PubSub api or the one on the staging log server
        private static readonly string PubSubApiUrl = "http://10.212.74.11:8080";
        //private static readonly string PubSubApiUrl = "http://localhost:8080";

        [HttpPost]
        [Route("subscribe")]
        [AllowAnonymous]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<RegularOptionResponseDTO>))]
        public async Task<IHttpActionResult> Subscribe([FromBody] SubscribeRequestWithTokenDTO request)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {request.Token}");
            var content = new StringContent(JsonConvert.SerializeObject(request.Subscription), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/subscribe", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            return Ok(responseContent);

            return Ok(responseContent);
        }

        [HttpPost]
        [Route("callback/{id}")]
        [AllowAnonymous]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<RegularOptionResponseDTO>))]
        public IHttpActionResult Callback(string id, [FromBody] string message)
        {
            Console.WriteLine($"callbackId: {id}, message: {message}");

            using var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes("local-no-secret"));
            var hash = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(message));
            var result = Convert.ToBase64String(hash);

            if (Request.Headers.TryGetValues("Signature-Header", out var signatureHeaders))
            {
                Console.WriteLine($"Signature-Header: {signatureHeaders.First()}");
            }
            else
            {
                Console.WriteLine("Signature-Header not found in the request.");
            }

            if (signatureHeaders.First() == result)
            {
                Console.WriteLine("Signature-Header matches the hash result.");
            }

            return Ok(signatureHeaders.First());
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(PubSubApiUrl);
            return client;
        }

        [HttpPost]
        [AllowAnonymous]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<RegularOptionResponseDTO>))]
        [Route("publish")]
        public async Task<IHttpActionResult> Publish(PublishRequestDTO request)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {request.Token}");
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/publish", content);
            return Ok(response);
        }
    }
}