using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Infrastructure.Services.Http;

public class KitosHttpClient : IKitosHttpClient
{

    private readonly IHttpClientFactory _httpClientFactory;

    public KitosHttpClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HttpResponseMessage> PostAsync(object content, Uri uri, string token)
    {
        var httpClient = _httpClientFactory.CreateClient();

        var serializedObject = JsonConvert.SerializeObject(content);
        var payload = new StringContent(serializedObject, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = payload
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await httpClient.SendAsync(request);
    }
}
