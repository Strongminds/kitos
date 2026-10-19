using System.Net;
using System.Security.Cryptography;

namespace Tools.Mcp.LocalClient
{
    public static class LocalClientHost
    {
        public static void Map(WebApplication app, Uri origin, Uri broker, PilotSession pilot)
        {
            var csrf = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var browserSession = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            app.Use(async (context, next) =>
            {
                context.Response.Headers.CacheControl = "no-store";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["Referrer-Policy"] = "strict-origin";
                context.Response.Headers["Content-Security-Policy"] = $"default-src 'none'; form-action 'self' {broker.GetLeftPart(UriPartial.Authority)}; frame-ancestors 'none'; base-uri 'none'";
                if (!context.Request.IsHttps || context.Request.Host != new HostString(origin.Authority) || context.Connection.RemoteIpAddress is not { } ip || !IPAddress.IsLoopback(ip))
                { await Reject(context); return; }
                if (context.Request.Path == "/" && context.Request.Cookies["pilot-session"] != browserSession)
                    context.Response.Cookies.Append("pilot-session", browserSession, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax });
                else if (context.Request.Cookies["pilot-session"] != browserSession)
                { await Reject(context); return; }
                if (context.Request.Method == "POST")
                {
                    if (!context.Request.HasFormContentType || context.Request.ContentLength is > 4096 ||
                        context.Request.Headers.TryGetValue("Origin", out var suppliedOrigin) && suppliedOrigin != origin.GetLeftPart(UriPartial.Authority))
                    { await Reject(context); return; }
                    var form = await context.Request.ReadFormAsync(context.RequestAborted);
                    if (form["csrf"].Count != 1 || form["csrf"] != csrf) { await Reject(context); return; }
                }
                await next(context);
            });
            app.MapGet("/", () => Results.Content(Page(pilot.Status, pilot.Result), "text/html"));
            app.MapPost("/connect", async () =>
            {
                var authorization = await pilot.ConnectAsync();
                return authorization is null ? Results.Redirect("/") : Results.Redirect(authorization.AbsoluteUri);
            });
            app.MapGet("/callback", (HttpContext context) =>
            {
                if (!pilot.CompleteAuthorization(context.Request.Query)) return Results.BadRequest("No valid pending authorization response.");
                return Results.Redirect("/");
            });
            app.MapPost("/query", async (HttpContext context) =>
            {
                var form = await context.Request.ReadFormAsync(context.RequestAborted);
                if (form["name"].Count != 1) return Results.BadRequest();
                await pilot.QueryAsync(form["name"].ToString(), context.RequestAborted);
                return Results.Redirect("/");
            });
            string Page(string status, string? result)
            {
                static string Encode(string? text) => WebUtility.HtmlEncode(text ?? string.Empty);
                return $"""
        <!doctype html><html lang="en"><head><meta charset="utf-8"><title>KITOS local MCP inspector</title></head><body>
        <h1>KITOS local MCP inspector</h1><p>{Encode(status)}</p>
        <p>Local protocol inspection only. Enter your KITOS token exclusively on the broker's login page. Results cover one page of accessible systems.</p>
        <form method="post" action="/connect"><input type="hidden" name="csrf" value="{csrf}"><button>Connect through KITOS broker</button></form>
        <form method="post" action="/query"><input type="hidden" name="csrf" value="{csrf}"><label>System name <input name="name" maxlength="256" required></label><button>Query systems</button></form>
        <p><a href="/">Refresh connection status</a></p><pre>{Encode(result)}</pre></body></html>
        """;
            }
            async Task Reject(HttpContext context)
            {
                await Results.Content(Page("This form was rejected. Reload the inspector page and try again.", null), "text/html", statusCode: 403).ExecuteAsync(context);
            }
        }
    }
}
