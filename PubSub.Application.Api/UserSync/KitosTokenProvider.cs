using System.Net.Http.Json;
using System.Text.Json;

namespace PubSub.Application.Api.UserSync;

/// <summary>Uses the normal KITOS API login. Tokens remain in memory only.</summary>
public class KitosTokenProvider(IHttpClientFactory clients, IConfiguration configuration, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private TokenResponse? _cached;
    private Uri? _cachedEndpoint;

    public void Invalidate() => _cached = null;

    public void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(configuration["UserSync:KitosEmail"]) ||
            string.IsNullOrWhiteSpace(configuration["UserSync:KitosPassword"]))
            throw new InvalidOperationException("UserSync requires KitosEmail and KitosPassword.");
    }

    public async Task<string> GetToken(Uri ingestionEndpoint, CancellationToken cancellationToken)
    {
        // Resolve against the same server: credentials are never sent to a separate configured host.
        var tokenEndpoint = new Uri(ingestionEndpoint, "/api/authorize/GetToken");
        if (_cached != null && _cachedEndpoint == tokenEndpoint && _cached.Expires > _time.GetUtcNow().AddMinutes(1))
            return _cached.Token;

        ValidateConfiguration();
        using var client = clients.CreateClient("UserSync");
        using var response = await client.PostAsJsonAsync(tokenEndpoint, new
        {
            Email = configuration["UserSync:KitosEmail"],
            Password = configuration["UserSync:KitosPassword"]
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        TokenResponse? token;
        try { token = (await response.Content.ReadFromJsonAsync<TokenResponseEnvelope>(cancellationToken))?.Response; }
        catch (JsonException) { throw new HttpRequestException("KITOS returned an invalid token response."); }
        if (token == null || !token.LoginSuccessful || string.IsNullOrWhiteSpace(token.Token) ||
            token.Expires <= _time.GetUtcNow().AddMinutes(1))
            throw new HttpRequestException("KITOS returned an invalid or expired token.");

        _cached = token;
        _cachedEndpoint = tokenEndpoint;
        return token.Token;
    }
}
