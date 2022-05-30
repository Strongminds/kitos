using System;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using Infrastructure.Serviceplatformen.OrganisationEnhed.Organisation;

namespace Core.DomainServices.FKOrganization
{
    internal static class FkOrganisationHelper
    {
        /// <summary>
        /// Create a request to list all org units in the organization
        /// </summary>
        /// <param name="municipalityCvr"></param>
        /// <returns></returns>
        public static laesRequest CreateOrganisationEnhedLaesRequest(string municipalityCvr)
        {
            var listRequest = new laesRequest()
            {
                LaesRequest1 = new LaesRequestType
                {
                    AuthorityContext = new AuthorityContextType
                    {
                        MunicipalityCVR = "58271713"//municipalityCvr //TODO: Use the cvr
                    },
                    LaesInput = new LaesInputType()
                    {
                        UUIDIdentifikator = "4C5C9482-CAB6-4A85-8491-88F98E61D161" //TODO: Ballerup this one
                    }
                }
            };
            return listRequest;
        }

        //TODO: Create a search by uuid request for orgs where we don't have the fk org uuid

        public static OrganisationPortTypeClient CreateOrganisationPortTypeClient(BasicHttpBinding binding, string urlServicePlatformService, X509Certificate2 certificate)
        {
            var client = new OrganisationPortTypeClient(binding, new EndpointAddress(urlServicePlatformService))
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
    }
}