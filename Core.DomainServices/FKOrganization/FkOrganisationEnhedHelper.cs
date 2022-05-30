using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using Infrastructure.Serviceplatformen.OrganisationEnhed.ServiceReference;

namespace Core.DomainServices.FKOrganization
{
    internal static class FkOrganisationEnhedHelper
    {
        public static listRequest CreateOrganisationEnhedListRequest(string municipalityCvr, params string[] currentUnitUuids)
        {
            var listRequest = new listRequest()
            {
                ListRequest1 = new ListRequestType
                {
                    AuthorityContext = new AuthorityContextType
                    {
                        MunicipalityCVR = "58271713"//municipalityCvr //TODO: Use the cvr
                    },
                    ListInput = new ListInputType
                    {
                        UUIDIdentifikator = currentUnitUuids
                    }
                }
            };
            return listRequest;
        }

        public static soegRequest CreateOrganisationEnhedSoegRequest(string municipalityCvr, string organizationUuid, int pageSize, int skip = 0)
        {
            //TODO: Paginering er muligt her
            var enhedSoegRequest = new soegRequest
            {
                SoegRequest1 = new SoegRequestType
                {
                    AuthorityContext = new AuthorityContextType
                    {
                        MunicipalityCVR = "58271713"//municipalityCvr //TODO: Use the cvr
                    },
                    SoegInput = new SoegInputType1
                    {
                        AttributListe = new AttributListeType()
                        {
                        },
                        TilstandListe = new TilstandListeType()
                        {

                        },
                        RelationListe = new RelationListeType
                        {
                            Tilhoerer = new OrganisationRelationType()
                            {
                                ReferenceID = new UnikIdType()
                                {
                                    Item = "4C5C9482-CAB6-4A85-8491-88F98E61D161", //TODO organizationUuid
                                    ItemElementName = ItemChoiceType.UUIDIdentifikator
                                }
                            }
                        },
                        MaksimalAntalKvantitet = pageSize.ToString("D"),
                        FoersteResultatReference = skip.ToString("D")
                    }
                }
            };
            return enhedSoegRequest;
        }

        public static OrganisationEnhedPortTypeClient CreateOrganisationPortTypeClient(BasicHttpBinding binding, string urlServicePlatformService, X509Certificate2 certificate)
        {
            var client = new OrganisationEnhedPortTypeClient(binding, new EndpointAddress(urlServicePlatformService))
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