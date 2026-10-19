using Mcp.ReadBoundary.Core;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using System.Net.Security;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Mcp.Broker
{
    public static class BrokerHost
    {
        public static WebApplication Build(string[] args, BrokerOptions options, Action<WebApplicationBuilder>? configure = null)
        {
            options.Validate();
            var builder = WebApplication.CreateBuilder(args);
            // OAuth/authentication components can log serialized protocol objects. Pilot emits no credential-bearing logs.
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(server =>
            {
                server.Limits.MaxRequestBodySize = 32768;
                server.ListenAnyIP(options.PublicOrigin.Port, listen => listen.UseHttps(options.ServerCertificate, tls =>
                {
                    tls.ClientCertificateMode = ClientCertificateMode.NoCertificate;
                }));
                server.ListenAnyIP(options.ReadPort, listen => listen.UseHttps(options.ServerCertificate, tls =>
                {
                    tls.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    tls.ClientCertificateValidation = (certificate, _, errors) =>
                        (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.None && options.IsWorker(certificate);
                }));
            });
            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<PilotState>();
            builder.Services.AddSingleton(provider => new KitosClient(new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }), options.KitosOrigin, TimeProvider.System, provider.GetRequiredService<PilotState>()));
            builder.Services.AddSingleton(_ => new WorkerClient(options.CreateWorkerClient()));
            builder.Services.AddAntiforgery(configuration =>
            {
                configuration.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                configuration.Cookie.HttpOnly = true;
                configuration.Cookie.SameSite = SameSiteMode.Strict;
            });
            builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
            builder.Services.AddAuthorization();
            builder.Services.AddOpenIddict().AddServer(configuration =>
            {
                configuration.EnableDegradedMode();
                configuration.SetIssuer(options.PublicOrigin);
                configuration.SetConfigurationEndpointUris("/.well-known/oauth-authorization-server", "/.well-known/openid-configuration");
                configuration.SetAuthorizationEndpointUris("/connect/authorize").SetTokenEndpointUris("/connect/token");
                configuration.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
                configuration.Configure(values =>
                {
                    values.ClientAuthenticationMethods.Clear();
                    values.ClientAuthenticationMethods.Add(ClientAuthenticationMethods.None);
                    values.CodeChallengeMethods.Clear();
                    values.CodeChallengeMethods.Add(CodeChallengeMethods.Sha256);
                });
                configuration.RegisterScopes("kitos.read");
                configuration.RegisterResources(options.Resource);
                configuration.AddSigningCertificate(options.SigningCertificate).AddEncryptionCertificate(options.EncryptionCertificate);
                configuration.SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(2));
                configuration.SetAccessTokenLifetime(TimeSpan.FromMinutes(5));
                configuration.UseAspNetCore().EnableAuthorizationEndpointPassthrough().EnableTokenEndpointPassthrough();
                if (options.EnableDynamicClientRegistration)
                    configuration.AddEventHandler<ApplyConfigurationResponseContext>(handler => handler.UseInlineHandler(context =>
                    {
                        context.Response.SetParameter("registration_endpoint", new Uri(options.PublicOrigin, "/connect/register").AbsoluteUri);
                        return default;
                    }).SetOrder(int.MinValue + 100000));
                configuration.AddEventHandler<ValidateAuthorizationRequestContext>(handler => handler.UseInlineHandler(context =>
                {
                    if (context.ClientId != options.ClientId || context.RedirectUri != options.RedirectUri ||
                        context.Request.ResponseType != ResponseTypes.Code || context.Request.CodeChallengeMethod != CodeChallengeMethods.Sha256 ||
                        context.Request.CodeChallenge?.Length != 43 || context.Request.Scope != "kitos.read" ||
                        !context.Request.GetResources().SequenceEqual(new[] { options.Resource })) context.Reject(Errors.InvalidRequest);
                    return default;
                }).SetOrder(int.MaxValue - 1000));
                configuration.AddEventHandler<ValidateTokenRequestContext>(handler => handler.UseInlineHandler(context =>
                {
                    var principal = context.AuthorizationCodePrincipal;
                    var state = context.Transaction.GetHttpRequest()?.HttpContext.RequestServices.GetRequiredService<PilotState>();
                    if (context.Request.ClientId != options.ClientId || context.Request.RedirectUri != options.RedirectUri ||
                        context.Request.ClientSecret is not null || context.Request.GetResources().Any() && !context.Request.GetResources().SequenceEqual(new[] { options.Resource }) ||
                        context.Request.Scope is not null && context.Request.Scope != "kitos.read" ||
                        context.Request.GrantType != GrantTypes.AuthorizationCode || principal is null ||
                        state?.GetCredential(principal.GetClaim(Claims.Subject)) is null ||
                        !state.RedeemCode(principal.GetClaim("pilot_code"))) context.Reject(Errors.InvalidGrant);
                    return default;
                }).SetOrder(OpenIddictServerHandlers.Exchange.ValidateCodeVerifier.Descriptor.Order + 1000));
            }).AddValidation(configuration =>
            {
                configuration.UseLocalServer();
                configuration.AddAudiences(options.Resource);
                configuration.UseAspNetCore();
            });
            // ASP.NET Core's post-configuration adds Basic by default; this pilot registers only a public client.
            builder.Services.PostConfigure<OpenIddictServerOptions>(values =>
            {
                values.ClientAuthenticationMethods.Clear();
                values.ClientAuthenticationMethods.Add(ClientAuthenticationMethods.None);
            });

            configure?.Invoke(builder);
            var app = builder.Build();
            app.Use(async (context, next) =>
            {
                context.Response.Headers.CacheControl = "no-store";
                context.Response.Headers["Referrer-Policy"] = "no-referrer";
                // Form submission redirects must reach the registered client's callback in Chromium.
                context.Response.Headers["Content-Security-Policy"] = $"default-src 'none'; form-action 'self' {new Uri(options.RedirectUri).GetLeftPart(UriPartial.Authority)}; frame-ancestors 'none'";
                var readPlane = context.Connection.LocalPort == options.ReadPort;
                // Listener identity is transport state; caller-supplied Host/forwarded headers cannot select the read plane.
                if (!context.Request.IsHttps ||
                    readPlane && (context.Request.Path != "/reads" || !HttpMethods.IsPost(context.Request.Method) ||
                        !options.IsWorker(await context.Connection.GetClientCertificateAsync(context.RequestAborted))) ||
                    !readPlane && (context.Request.Host != new HostString(options.PublicOrigin.Authority) || context.Request.Path == "/reads"))
                { context.Response.StatusCode = 403; return; }
                if (context.Request.Path == "/mcp" && context.Request.Headers.TryGetValue("Origin", out var origin) &&
                    (origin.Count != 1 || origin[0] != options.PublicOrigin.GetLeftPart(UriPartial.Authority) && origin[0] != new Uri(options.RedirectUri).GetLeftPart(UriPartial.Authority)))
                { context.Response.StatusCode = 403; return; }
                try { await next(context); }
                catch (BadHttpRequestException error)
                { if (!context.Response.HasStarted) { context.Response.Clear(); context.Response.StatusCode = error.StatusCode is 400 or 413 ? error.StatusCode : 400; } }
                catch (DecoderFallbackException)
                { if (!context.Response.HasStarted) { context.Response.Clear(); context.Response.StatusCode = 400; } }
                catch (Exception error) when (error is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException or IOException)
                { if (!context.Response.HasStarted) { context.Response.Clear(); context.Response.StatusCode = 502; } }
            });
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapMethods("/connect/authorize", ["GET", "POST"], Authorize);
            if (options.EnableDynamicClientRegistration) app.MapPost("/connect/register", RegisterClient);
            app.MapPost("/connect/token", async (HttpContext context) =>
            {
                var authentication = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                if (!authentication.Succeeded || authentication.Principal is null) return Results.StatusCode(401);
                var credential = context.RequestServices.GetRequiredService<PilotState>().GetCredential(authentication.Principal.GetClaim(Claims.Subject));
                if (credential is null) return Results.StatusCode(401);
                authentication.Principal.SetAccessTokenLifetime(TimeSpan.FromSeconds(Math.Min(300, (credential.Expires - DateTimeOffset.UtcNow).TotalSeconds)));
                return Results.SignIn(authentication.Principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            });
            app.MapGet("/disconnect", (HttpContext context, IAntiforgery antiforgery) =>
            {
                var token = antiforgery.GetAndStoreTokens(context);
                var encode = HtmlEncoder.Default;
                return Results.Content("<html><body><h1>Disconnect KITOS broker access</h1><form method='post'><input type='hidden' name='" +
                    encode.Encode(token.FormFieldName) + "' value='" + encode.Encode(token.RequestToken ?? "") +
                    "'><label>KITOS JWT <input type='password' name='jwt' autocomplete='off' required></label><button>Revoke broker access</button></form></body></html>", "text/html");
            });
            app.MapPost("/disconnect", async (HttpContext context, PilotState state, IAntiforgery antiforgery, KitosClient kitos) =>
            {
                var authenticated = await context.AuthenticateAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                if (authenticated.Succeeded && authenticated.Principal is not null) state.Revoke(authenticated.Principal.GetClaim(Claims.Subject));
                else
                {
                    try { await antiforgery.ValidateRequestAsync(context); }
                    catch (AntiforgeryValidationException) { return Results.StatusCode(403); }
                    var form = await context.Request.ReadFormAsync(context.RequestAborted);
                    if (form["jwt"].Count != 1) return Results.StatusCode(403);
                    var identity = await kitos.Authenticate(form["jwt"].ToString(), context.RequestAborted);
                    if (identity is null) return Results.StatusCode(401);
                    state.RevokeUser(identity.Value.User);
                }
                return Results.StatusCode(204);
            });
            app.MapGet("/.well-known/oauth-protected-resource/mcp", () => Results.Json(new { resource = options.Resource, authorization_servers = new[] { options.PublicOrigin.AbsoluteUri }, scopes_supported = new[] { "kitos.read" } }));
            app.MapMethods("/mcp", ["GET", "POST", "DELETE"], Proxy);
            app.MapPost("/reads", Reads);
            return app;
        }

        private static async Task<IResult> RegisterClient(HttpContext context, BrokerOptions options)
        {
            static IResult Invalid() => Results.Json(new { error = "invalid_client_metadata" }, statusCode: 400);
            if (!context.Request.HasJsonContentType() || context.Request.QueryString.HasValue) return Invalid();
            JsonDocument document;
            try { document = JsonDocument.Parse(await ReadBody(context, 4096), new JsonDocumentOptions { MaxDepth = 4 }); }
            catch (JsonException) { return Invalid(); }
            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return Invalid();
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in root.EnumerateObject())
                {
                    if (!names.Add(property.Name)) return Invalid();
                    var valid = property.Name switch
                    {
                        "redirect_uris" => Single(property.Value, options.RedirectUri),
                        "token_endpoint_auth_method" => Text(property.Value, "none"),
                        "grant_types" => Single(property.Value, "authorization_code"),
                        "response_types" => Single(property.Value, "code"),
                        "scope" => Text(property.Value, "kitos.read"),
                        // Display metadata is ignored; URL values are never fetched or trusted for registration.
                        "client_name" or "client_uri" or "logo_uri" or "tos_uri" or "policy_uri" or "software_id" or "software_version" => property.Value.ValueKind == JsonValueKind.String,
                        "contacts" => property.Value.ValueKind == JsonValueKind.Array && property.Value.EnumerateArray().All(value => value.ValueKind == JsonValueKind.String),
                        _ => false
                    };
                    if (!valid) return Invalid();
                }
                if (!names.Contains("redirect_uris") || !names.Contains("token_endpoint_auth_method")) return Invalid();
            }
            // This opt-in pilot endpoint only describes its single preconfigured public client. It adds no authority or state.
            return Results.Json(new
            {
                client_id = options.ClientId,
                redirect_uris = new[] { options.RedirectUri },
                token_endpoint_auth_method = "none",
                grant_types = new[] { "authorization_code" },
                response_types = new[] { "code" },
                scope = "kitos.read"
            }, statusCode: 201);

            static bool Text(JsonElement value, string expected) => value.ValueKind == JsonValueKind.String && value.GetString() == expected;
            static bool Single(JsonElement value, string expected) => value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 1 && Text(value[0], expected);
        }

        private static async Task<IResult> Authorize(HttpContext context, IAntiforgery antiforgery, PilotState state, KitosClient kitos)
        {
            var request = context.GetOpenIddictServerRequest();
            if (request is null) return Results.StatusCode(400);
            if (HttpMethods.IsGet(context.Request.Method))
            {
                var token = antiforgery.GetAndStoreTokens(context);
                var encode = HtmlEncoder.Default;
                var hidden = string.Concat(request.GetParameters().Where(pair => pair.Key is "client_id" or "redirect_uri" or "response_type" or "scope" or "resource" or "code_challenge" or "code_challenge_method" or "state" or "response_mode")
                    .Select(pair => "<input type='hidden' name='" + encode.Encode(pair.Key) + "' value='" + encode.Encode(pair.Value.ToString() ?? "") + "'>"));
                var options = context.RequestServices.GetRequiredService<BrokerOptions>();
                return Results.Content("<html><body><h1>Connect KITOS read access</h1><p>Client " + encode.Encode(options.ClientId) + " requests " + encode.Encode(options.Resource) +
                    ". Paste your existing KITOS API JWT. This authorizes only data visible to the bearer account represented by that JWT, for five minutes. Revoke at /disconnect.</p>" +
                    "<form method='post'>" + hidden + "<input type='hidden' name='" + encode.Encode(token.FormFieldName) + "' value='" + encode.Encode(token.RequestToken ?? "") +
                    "'><label>KITOS JWT <input type='password' name='jwt' autocomplete='off' required></label>" +
                    "<label><input type='checkbox' name='consent' value='yes' required>Allow read-only access</label><button type='submit'>Connect</button></form></body></html>", "text/html");
            }
            try { await antiforgery.ValidateRequestAsync(context); }
            catch (AntiforgeryValidationException) { return Results.StatusCode(403); }
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            if (form["consent"] != "yes" || form["jwt"].Count != 1) return Results.StatusCode(403);
            var verified = await kitos.Authenticate(form["jwt"].ToString(), context.RequestAborted);
            if (verified is null) return Results.StatusCode(401);
            var credential = state.Enroll(verified.Value.User, form["jwt"].ToString(), verified.Value.Expires);
            var nonce = state.IssueCode();
            if (credential is null || nonce is null) return Results.StatusCode(429);
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            identity.SetClaim(Claims.Subject, credential.Id).SetClaim("pilot_code", nonce);
            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes("kitos.read");
            principal.SetResources(context.RequestServices.GetRequiredService<BrokerOptions>().Resource);
            principal.SetAccessTokenLifetime(TimeSpan.FromSeconds(Math.Min(300, (credential.Expires - DateTimeOffset.UtcNow).TotalSeconds)));
            identity.FindFirst(Claims.Subject)?.SetDestinations(Destinations.AccessToken);
            return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        private static async Task<IResult> Proxy(HttpContext context, PilotState state, WorkerClient worker, BrokerOptions options)
        {
            var authenticated = await context.AuthenticateAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            if (!authenticated.Succeeded || authenticated.Principal is null ||
                !authenticated.Principal.HasScope("kitos.read") || authenticated.Principal.GetExpirationDate() is not { } expiry)
            {
                context.Response.Headers.WWWAuthenticate = "Bearer resource_metadata=\"" + new Uri(options.PublicOrigin, "/.well-known/oauth-protected-resource/mcp") + "\"";
                return Results.StatusCode(401);
            }
            var credential = state.GetCredential(authenticated.Principal.GetClaim(Claims.Subject));
            if (credential is null) return Results.StatusCode(401);
            var grant = state.IssueGrant(credential.Id, expiry);
            if (grant is null) return Results.StatusCode(429);
            using var forwarded = new HttpRequestMessage(new HttpMethod(context.Request.Method), new Uri(options.WorkerOrigin, "/mcp"));
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            timeout.CancelAfter(TimeSpan.FromSeconds(12));
            forwarded.Headers.Add("X-Mcp-Read-Grant", grant);
            foreach (var name in new[] { "Accept", "MCP-Protocol-Version", "Mcp-Method", "Mcp-Name" })
                if (context.Request.Headers.TryGetValue(name, out var value)) forwarded.Headers.TryAddWithoutValidation(name, value.ToArray());
            if (HttpMethods.IsPost(context.Request.Method))
            {
                forwarded.Content = new ByteArrayContent(await ReadBody(context, 32768));
                forwarded.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType ?? "application/json");
            }
            using var response = await worker.Client.SendAsync(forwarded, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode) return Results.StatusCode((int)response.StatusCode is 401 or 403 or 429 ? (int)response.StatusCode : 502);
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            var bytes = await BoundedBytes(stream, 2 * 1024 * 1024, timeout.Token);
            if (bytes is null) return Results.StatusCode(502);
            context.Response.StatusCode = (int)response.StatusCode;
            return Results.Bytes(bytes, response.Content.Headers.ContentType?.ToString() ?? "application/json");
        }

        private static async Task<IResult> Reads(HttpContext context, BrokerOptions options, PilotState state, KitosClient kitos)
        {
            if (!options.IsWorker(await context.Connection.GetClientCertificateAsync(context.RequestAborted)) ||
                context.Request.Headers["X-Mcp-Read-Grant"].Count != 1) return Results.StatusCode(403);
            var credential = state.Resolve(context.Request.Headers["X-Mcp-Read-Grant"].ToString());
            if (credential is null) return Results.StatusCode(401);
            var body = new UTF8Encoding(false, true).GetString(await ReadBody(context, ReadRequestPolicy.MaxJsonBytes));
            var validated = new ReadRequestPolicy(options.KitosOrigin).ValidateJson(body);
            if (validated.Failed) return Results.StatusCode(403);
            using var document = JsonDocument.Parse(body);
            var parameters = document.RootElement.GetProperty("parameters");
            if (parameters.TryGetProperty("page", out var page) && page.GetInt32() != 0 ||
                parameters.TryGetProperty("pageSize", out var size) && size.GetInt32() != 100 ||
                parameters.TryGetProperty("nameContains", out var name) && string.IsNullOrWhiteSpace(name.GetString())) return Results.StatusCode(403);
            using var admission = state.Admit(credential.User);
            if (admission is null) return Results.StatusCode(429);
            var result = await kitos.Read(validated.Value, credential, context.RequestAborted);
            return result is null ? Results.StatusCode(502) : Results.Json(result);
        }

        private static async Task<byte[]> ReadBody(HttpContext context, int limit)
        {
            using var data = new MemoryStream();
            var buffer = new byte[4096];
            int read;
            while ((read = await context.Request.Body.ReadAsync(buffer, context.RequestAborted)) != 0)
            {
                if (data.Length + read > limit) throw new BadHttpRequestException("Request too large.", 413);
                data.Write(buffer, 0, read);
            }
            return data.ToArray();
        }
        private static async Task<byte[]?> BoundedBytes(Stream stream, int limit, CancellationToken cancellation)
        {
            using var data = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(buffer, cancellation)) != 0)
            {
                if (data.Length + read > limit) return null;
                data.Write(buffer, 0, read);
            }
            return data.ToArray();
        }
        internal sealed record WorkerClient(HttpClient Client);
    }
}
