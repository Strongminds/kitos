using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presentation.Web.Infrastructure.Configuration
{
    public static class AuthenticationServiceCollectionExtensions
    {
        private const string MultiScheme = "CookieOrJwt";

        /// <summary>
        /// Registers cookie + JWT bearer authentication and returns the signing key so it can
        /// be reused by other registrations (e.g. KitosServiceRegistration).
        /// </summary>
        public static SymmetricSecurityKey AddKitosAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var securityKeyString = configuration["AppSettings:SecurityKeyString"] ?? "";
            var baseUrl = configuration["AppSettings:BaseUrl"] ?? "https://localhost:44300/";

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKeyString))
            {
                // KeyId is required so the JWT includes a 'kid' header.
                // JsonWebTokenHandler (used by AddJwtBearer) throws IDX10517
                // when the token has no 'kid' and the validation key has no KeyId.
                KeyId = "kitos-jwt"
            };

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = MultiScheme;
                    options.DefaultChallengeScheme = MultiScheme;
                })
                .AddPolicyScheme(MultiScheme, "Cookie or JWT", options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        var auth = context.Request.Headers[Microsoft.Net.Http.Headers.HeaderNames.Authorization].FirstOrDefault();
                        // Route to JWT Bearer whenever an Authorization header is present.
                        // Cookie auth never uses the Authorization header, so this is safe.
                        // This also handles the Swagger UI ApiKey scheme which sends the raw token
                        // without the "Bearer " prefix.
                        if (!string.IsNullOrEmpty(auth))
                            return JwtBearerDefaults.AuthenticationScheme;
                        return CookieAuthenticationDefaults.AuthenticationScheme;
                    };
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        ValidIssuer = baseUrl,
                        ValidateIssuer = true,
                        IssuerSigningKey = signingKey,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        // JsonWebTokenHandler (.NET 8+) defaults ClaimsIdentity.AuthenticationType to
                        // "AuthenticationTypes.Federation" which OwinAuthenticationContextFactory does not
                        // recognise. Setting it to "Bearer" keeps the existing switch mapping working.
                        AuthenticationType = JwtBearerDefaults.AuthenticationScheme,
                        // JsonWebTokenHandler does not apply inbound claim type mapping, so the JWT "name"
                        // claim stays as "name". Setting NameClaimType ensures Identity.Name resolves it.
                        NameClaimType = "name",
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var auth = context.Request.Headers[Microsoft.Net.Http.Headers.HeaderNames.Authorization].FirstOrDefault();
                            // If the token is sent without "Bearer " prefix (e.g. from Swagger UI),
                            // extract it directly so JWT validation still works.
                            if (!string.IsNullOrEmpty(auth) && !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                                context.Token = auth;
                            return Task.CompletedTask;
                        }
                    };
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.LoginPath = "/account/login";
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = ctx =>
                        {
                            ctx.Response.StatusCode = 401;
                            return Task.CompletedTask;
                        },
                        OnRedirectToAccessDenied = ctx =>
                        {
                            ctx.Response.StatusCode = 403;
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();

            return signingKey;
        }
    }
}
