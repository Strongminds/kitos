using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PubSub.Core.Services.CallbackAuthentication;

namespace PubSub.Core.Services.Notifier
{
    public class HttpSubscriberNotifierService : ISubscriberNotifierService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICallbackAuthenticator _callbackAuthenticator;

        public HttpSubscriberNotifierService(IHttpClientFactory httpClientFactory, ICallbackAuthenticator callbackAuthenticator)
        {
            _httpClientFactory = httpClientFactory;
            _callbackAuthenticator = callbackAuthenticator;
        }
        public async Task Notify(JsonElement message, string recipient)
        {
            using var httpClient = _httpClientFactory.CreateClient();

            // Wrap the original JSON element in an object with a "Payload" property.
            var payloadWrapper = new { Payload = message };

            // Serialize the wrapper object.
            string jsonPayload = JsonSerializer.Serialize(payloadWrapper);

            // Use the serialized JSON for authentication.
            var authentication = _callbackAuthenticator.GetAuthentication(jsonPayload);
            httpClient.DefaultRequestHeaders.Add("Signature-Header", authentication);

            // Create the HTTP content.
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            await httpClient.PostAsync(recipient, content);
        }


    }
}
