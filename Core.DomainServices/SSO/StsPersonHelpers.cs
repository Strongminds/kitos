﻿using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using Infrastructure.Soap.STSPerson;
using LaesInputType = Infrastructure.Soap.STSPerson.LaesInputType;
using laesRequest = Infrastructure.Soap.STSPerson.laesRequest;
using LivscyklusKodeType = Infrastructure.Soap.STSPerson.LivscyklusKodeType;
using RegistreringType1 = Infrastructure.Soap.STSPerson.RegistreringType1;

namespace Core.DomainServices.SSO
{
    internal static class StsPersonHelpers
    {
        public static laesRequest CreateStsPersonLaesRequest(string uuid)
        {
            var laesInputType = new LaesInputType {UUIDIdentifikator = uuid};
            var laesRequest = new laesRequest
            {
                LaesInput = laesInputType
            };
            return laesRequest;
        }

        public static PersonPortTypeClient CreatePersonPortTypeClient(BasicHttpBinding binding, string urlServicePlatformService, X509Certificate2 certificate)
        {
            var client = new PersonPortTypeClient(binding, new EndpointAddress(urlServicePlatformService))
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

        public static bool IsStsPersonObsolete(this RegistreringType1 registreringType1)
        {
            return registreringType1.LivscyklusKode.Equals(LivscyklusKodeType.Slettet) ||
                   registreringType1.LivscyklusKode.Equals(LivscyklusKodeType.Passiveret);
        }
    }}
