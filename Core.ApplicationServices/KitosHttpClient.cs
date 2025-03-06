using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Core.ApplicationServices;

public class KitosHttpClient : IKitosHttpClient
{

    private readonly HttpClient _httpClient;
    private readonly IKitosInternalTokenIssuer _tokenIssuer;

    public KitosHttpClient(HttpClient httpClient, IKitosInternalTokenIssuer tokenIssuer)
    {
        _httpClient = httpClient;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<HttpResponseMessage> PostAsync(object content, Uri uri)
    {
        var token = _tokenIssuer.GetToken();

        var serializedObject = JsonConvert.SerializeObject(content);
        var payload = new StringContent(serializedObject, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = payload
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return await _httpClient.SendAsync(request);
    }
}
