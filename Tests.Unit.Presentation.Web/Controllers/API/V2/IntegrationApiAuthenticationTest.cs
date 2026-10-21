using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Presentation.Web.Controllers.API.V2.Internal.Users;
using Xunit;

namespace Tests.Unit.Presentation.Web.Controllers.API.V2;

public class IntegrationApiAuthenticationTest
{
    [Theory]
    [InlineData("/api/v2/integrations/fk-organisation/user-changes")]
    [InlineData("/api/v2/integration/beskedfordeler")]
    public async Task RemovedIngestionRoute_ReturnsNotFound(string route)
    {
        using var host = await new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services => services.AddControllers()
                .AddApplicationPart(typeof(GlobalUserInternalV2Controller).Assembly))
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            })).StartAsync();
        using var client = host.GetTestClient();
        using var response = await client.PostAsync(route, new StringContent("{}"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(typeof(GlobalUserInternalV2Controller).GetMethods(), x => x.Name.Contains("PubSub", StringComparison.Ordinal));
        Assert.Null(typeof(Core.DomainModel.User).GetProperty("IsPubSubUser"));
    }
}
