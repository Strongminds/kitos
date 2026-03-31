using System;
using System.IdentityModel.Tokens;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using Core.Abstractions.Types;
using Digst.OioIdws.SoapCore;
using Infrastructure.STS.Common.Model.Token;
using Kombit.InfrastructureSamples.AdresseService;
using Kombit.InfrastructureSamples.BrugerService;
using Kombit.InfrastructureSamples.PersonService;
using Kombit.InfrastructureSamples.Token;

namespace Core.DomainServices.SSO
{
    public static class PortFactory
    {
        private const string ERROR_RECEIVED_USER_CONTEXT_DOES_NOT_EXIST_ON_USING_SYSTEM = "5015";

        public static Result<BrugerPortType, string> CreateBrugerPort(TokenFetcher tokenFetcher, StsOrganisationIntegrationConfiguration configuration, string cvrNumber)
        {
            return ChannelWithIssuedToken<BrugerPortType>(tokenFetcher, configuration,
                configuration.GetOrganisationServiceUrl("bruger"), cvrNumber);
        }

        public static Result<AdressePortType, string> CreateAdressePort(TokenFetcher tokenFetcher, StsOrganisationIntegrationConfiguration configuration, string cvrNumber)
        {
            return ChannelWithIssuedToken<AdressePortType>(tokenFetcher, configuration,
                configuration.GetOrganisationServiceUrl("adresse"), cvrNumber);
        }

        public static Result<PersonPortType, string> CreatePersonPort(TokenFetcher tokenFetcher, StsOrganisationIntegrationConfiguration configuration, string cvrNumber)
        {
            return ChannelWithIssuedToken<PersonPortType>(tokenFetcher, configuration,
                configuration.GetOrganisationServiceUrl("person"), cvrNumber);
        }

        private static Result<T, string> ChannelWithIssuedToken<T>(TokenFetcher tokenFetcher, StsOrganisationIntegrationConfiguration configuration, string serviceEndpointUrl, string cvrNumber) where T : class
        {
            try
            {
                var token = (GenericXmlSecurityToken)tokenFetcher.IssueToken(configuration.OrgService6EntityId, cvrNumber);
                var config = tokenFetcher.BuildServiceConfig(configuration.OrgService6EntityId, serviceEndpointUrl, cvrNumber);
                return FederatedChannelFactoryExtensions.CreateChannelWithIssuedToken<T>(token, config);
            }
            catch (FaultException e)
            {
                if (e.Code.Name == ERROR_RECEIVED_USER_CONTEXT_DOES_NOT_EXIST_ON_USING_SYSTEM)
                {
                    return $"No service agreement on municipality with CVR {cvrNumber}";
                }
                return $"SSO unknown general error: {e.Message}";
            }
        }
    }
}