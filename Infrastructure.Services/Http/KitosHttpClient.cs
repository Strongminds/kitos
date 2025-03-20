using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace Infrastructure.Services.Http;

public class KitosHttpClient : IKitosHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public KitosHttpClient(ILogger logger)
    {
        _httpClient = new HttpClient();
        _logger = logger;
    }

    public async Task<HttpResponseMessage> PostAsync(object content, Uri uri, string token)
    {

        var serializedObject = JsonConvert.SerializeObject(content);
        var payload = new StringContent(serializedObject, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = payload
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await _httpClient.SendAsync(request);
            _logger.Fatal($"Post result. Url: {uri}, payload: {serializedObject}, response code: {response.StatusCode}, response: {response}");
        }
        catch (Exception ex)
        {
            _logger.Fatal($"Could not post content. Url: {uri}, payload: {serializedObject}, exception: {ex}");
            throw ex;
        }
    }
}