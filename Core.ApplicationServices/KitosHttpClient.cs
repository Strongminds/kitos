using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Core.ApplicationServices;

public class KitosHttpClient : IKitosHttpClient
{

    private readonly HttpClient _httpClient;

    public KitosHttpClient(HttpClient httpClient, IKitosInternalTokenIssuer internalKitosTokenIssuer)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpResponseMessage> PostAsync(object content, Uri uri)
    {
        var serializedObject = JsonConvert.SerializeObject(content);
        var payload = new StringContent(serializedObject, Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync(uri.AbsoluteUri, payload);
    }
}
