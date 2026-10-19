using Mcp.Broker;
using System.Security.Cryptography.X509Certificates;

var configuration = new ConfigurationBuilder().AddEnvironmentVariables("MCP_BROKER_").Build();
string Required(string name) => configuration[name] is { Length: > 0 } value ? value : throw new InvalidOperationException($"Missing broker configuration {name}.");
X509Certificate2 Certificate(string name) => X509CertificateLoader.LoadPkcs12FromFile(Required(name + "Path"), configuration[name + "Password"]);
var options = new BrokerOptions
{
    PublicOrigin = new Uri(Required("PublicOrigin")),
    ReadPort = configuration["ReadPort"] is { } readPort ? int.Parse(readPort, System.Globalization.CultureInfo.InvariantCulture) : 7446,
    WorkerOrigin = new Uri(Required("WorkerOrigin")),
    KitosOrigin = new Uri(Required("KitosOrigin")),
    ClientId = Required("ClientId"),
    RedirectUri = Required("RedirectUri"),
    EnableDynamicClientRegistration = configuration["EnableDynamicClientRegistration"] is { } registration ? bool.Parse(registration) : false,
    ResourceIdentifier = configuration["ResourceIdentifier"] is { Length: > 0 } resourceIdentifier ? resourceIdentifier : null,
    ServerCertificate = Certificate("ServerCertificate"),
    WorkloadCertificate = Certificate("WorkloadCertificate"),
    SigningCertificate = Certificate("SigningCertificate"),
    EncryptionCertificate = Certificate("EncryptionCertificate"),
    WorkerClientCertificateSha256 = Required("WorkerClientCertificateSha256"),
    WorkerServerCertificateSha256 = Required("WorkerServerCertificateSha256")
};
await BrokerHost.Build(args, options).RunAsync();
