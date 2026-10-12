using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace PubSub.Application.Api.UserSync;

public sealed class KitosUserChangeDelivery(IHttpClientFactory clients, IConfiguration configuration)
{
    // One sequential consumer owns this instance and its token cache.
    private readonly KitosTokenProvider _tokens = new(clients, configuration);

    public Uri ValidateConfiguration()
    {
        if (!Uri.TryCreate(configuration["UserSync:KitosEndpoint"], UriKind.Absolute, out var endpoint) || endpoint.Scheme != "https")
            throw new InvalidOperationException("UserSync requires an HTTPS KitosEndpoint.");
        _tokens.ValidateConfiguration();
        return endpoint;
    }

    public async Task<HttpStatusCode> Send(Uri endpoint, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        if (endpoint.Scheme != "https") throw new InvalidOperationException("UserSync requires HTTPS.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(90));
        using var client = clients.CreateClient("UserSync");
        for (var attempt = 0; ; attempt++)
        {
            var token = await _tokens.GetToken(endpoint, timeout.Token);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new StringContent(Encoding.UTF8.GetString(body.Span), Encoding.UTF8, "application/json");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request, timeout.Token);
            if (response.StatusCode != HttpStatusCode.Unauthorized) return response.StatusCode;
            _tokens.Invalidate();
            if (attempt == 1) return response.StatusCode;
        }
    }

    public static bool IsPermanentFailure(HttpStatusCode status) =>
        (int)status is >= 400 and < 500 && status is not (HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
}
