using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Tools.Mcp.LocalClient;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
var origin = new Uri("https://localhost:7445");
var broker = new Uri(builder.Configuration["LocalClient:BrokerOrigin"] ?? "https://localhost:7443");
if (broker.Scheme != "https" || broker.AbsolutePath != "/" || broker.Query.Length != 0 || broker.Fragment.Length != 0 || broker.UserInfo.Length != 0)
    throw new InvalidOperationException("LocalClient requires a fixed HTTPS broker origin.");
using var serverCertificate = X509CertificateLoader.LoadPkcs12FromFile(builder.Configuration["LocalClient:ServerCertificatePath"] ?? throw new InvalidOperationException("LocalClient server certificate path is required."), null);
using var brokerCertificate = X509Certificate2.CreateFromPem(File.ReadAllText(builder.Configuration["LocalClient:BrokerCertificatePath"] ?? throw new InvalidOperationException("LocalClient broker public certificate path is required.")));
builder.WebHost.ConfigureKestrel(server =>
{
    server.Limits.MaxRequestBodySize = 4096;
    server.Listen(IPAddress.Loopback, 7445, listener => listener.UseHttps(serverCertificate));
});
await using var pilot = new PilotSession(broker, origin, brokerCertificate.GetCertHashString(HashAlgorithmName.SHA256));
var app = builder.Build();
LocalClientHost.Map(app, origin, broker, pilot);
await app.RunAsync();
