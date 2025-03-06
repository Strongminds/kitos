using Core.ApplicationServices.Authentication;
using Core.ApplicationServices.Model.KitosEvents;
using Infrastructure.Services.Http;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace Core.ApplicationServices.KitosEvents;

public class HttpEventPublisher : IHttpEventPublisher
{
    private readonly IKitosHttpClient _httpClient;
    private readonly IKitosInternalTokenIssuer _tokenIssuer;
    private readonly string _baseUrl;
    private const string PublishEndpoint = "api/publish";
    public HttpEventPublisher(IKitosHttpClient httpClient, IKitosInternalTokenIssuer tokenIssuer, string baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
        _tokenIssuer = tokenIssuer;

        _baseUrl = "https://localhost:7226/"; //Temporary
    }

    public async Task<HttpResponseMessage> PostEventAsync(KitosEventDTO eventDTO)
    {
        var token = _tokenIssuer.GetToken();
        var url = new Uri(new Uri(_baseUrl), PublishEndpoint);
        return await _httpClient.PostAsync(eventDTO, url, token.Value);
    }
}