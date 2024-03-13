using System;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using Digst.OioIdws.Soap.Bindings;
using Kombit.InfrastructureSamples.BrugerService;

namespace Core.DomainServices.SSO
{
    internal static class StsBrugerHelpers
    {
        public static laesRequest CreateStsBrugerLaesRequest(Guid uuid)
        {
            var laesInputType = new LaesInputType {UUIDIdentifikator = uuid.ToString()};
            var laesRequest = new laesRequest
            {
                RequestHeader = new RequestHeaderType { TransactionUUID = Guid.NewGuid().ToString() },
                LaesInput = laesInputType,
            };
            return laesRequest;
        }

        public static BrugerPortTypeClient CreateBrugerPortTypeClient(SoapBinding binding, string urlServicePlatformService, X509Certificate2 certificate)
        {
            var client = new BrugerPortTypeClient(binding, new EndpointAddress(urlServicePlatformService))
            {
                ClientCredentials =
                {
                    ClientCertificate =
                    {
                        Certificate = certificate
                    }
                }
            };
            return client;
        }

        public static bool IsStsBrugerObsolete(this RegistreringType5 registreringType1)
        {
            return registreringType1.LivscyklusKode.Equals(LivscyklusKodeType.Slettet) ||
                   registreringType1.LivscyklusKode.Equals(LivscyklusKodeType.Passiveret);
        }
    }
}