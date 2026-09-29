using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Core.ApplicationServices.Authentication;
using Core.ApplicationServices.Users;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Core.DomainServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Presentation.Web.Controllers.API.V2.Integration;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Infrastructure.Configuration;
using Presentation.Web.Infrastructure.Factories.Authentication;
using Serilog;
using Xunit;

namespace Tests.Unit.Presentation.Web.Controllers.API.V2;

public class IntegrationApiAuthenticationTest
{
    private const string Route = "/api/v2/integrations/fk-organisation/user-changes";

    [Fact]
    public async Task Pipeline_Requires_Token_And_Current_PubSub_Permission_And_Hides_Endpoint()
    {
        var user = new User { Id = 17, HasApiAccess = true, IsPubSubUser = true };
        user.OrganizationRights.Add(new OrganizationRight { OrganizationId = 1, Role = OrganizationRole.User });
        var repository = new Mock<IUserRepository>();
        repository.Setup(x => x.GetById(user.Id)).Returns(() => user);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppSettings:SecurityKeyString"] = "test-signing-key-with-at-least-thirty-two-bytes",
            ["AppSettings:BaseUrl"] = "https://kitos.test/"
        }).Build();
        string token = null!;
        using var host = await new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                var key = services.AddKitosAuthentication(config);
                token = new TokenValidator("https://kitos.test/", key).CreateToken(user).Value;
                services.AddHttpContextAccessor();
                services.AddSingleton(repository.Object);
                services.AddSingleton<ILogger>(new LoggerConfiguration().CreateLogger());
                services.AddScoped<IAuthenticationContextFactory, OwinAuthenticationContextFactory>();
                services.AddScoped<IAuthenticationContext>(sp => sp.GetRequiredService<IAuthenticationContextFactory>().Create());
                // Valid authentication reaches body validation; this test never invokes ingestion.
                services.AddSingleton(new ExternalUserChangeIngestionService(null!, null!, null!));
                services.AddControllers(options => options.Filters.Add<RequireValidatedCSRFAttributed>())
                    .AddApplicationPart(typeof(ExternalUserChangeIngestionController).Assembly).AddNewtonsoftJson();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            })).StartAsync();
        var server = host.GetTestServer();
        using var client = server.CreateClient();
        async Task<HttpStatusCode> Send(string? bearer)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Route) { Content = new StringContent("{}") };
            if (bearer != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.Unauthorized, await Send(null));
        Assert.Equal(HttpStatusCode.Unauthorized, await Send("invalid-token"));
        var cookieOptions = server.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, user.Id.ToString()) }, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties(), CookieAuthenticationDefaults.AuthenticationScheme);
        client.DefaultRequestHeaders.Add("Cookie", $"{cookieOptions.Cookie.Name}={cookieOptions.TicketDataFormat.Protect(ticket)}");
        Assert.Equal(HttpStatusCode.Unauthorized, await Send(null));
        client.DefaultRequestHeaders.Remove("Cookie");
        Assert.Equal(HttpStatusCode.BadRequest, await Send(token));
        user.IsPubSubUser = false;
        Assert.Equal(HttpStatusCode.Forbidden, await Send(token));
        user.IsSystemIntegrator = true;
        user.IsGlobalAdmin = true;
        Assert.Equal(HttpStatusCode.Forbidden, await Send(token));
        user.IsPubSubUser = true;
        user.HasApiAccess = false;
        Assert.Equal(HttpStatusCode.Forbidden, await Send(token));
        user.HasApiAccess = true;
        user.Deleted = true;
        Assert.Equal(HttpStatusCode.Forbidden, await Send(token));
        user.Deleted = false;
        user.IsGlobalAdmin = false;
        user.OrganizationRights.Clear();
        Assert.Equal(HttpStatusCode.Forbidden, await Send(token));

        var descriptions = server.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        Assert.DoesNotContain(descriptions.ApiDescriptionGroups.Items.SelectMany(x => x.Items),
            x => x.RelativePath == Route.TrimStart('/'));
    }
}
