using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using Core.Abstractions.Types;
using Core.DomainServices.Repositories.Organization;
using Core.DomainServices.SSO;
using Infrastructure.Serviceplatformen.OrganisationEnhed.ServiceReference;
using Serilog;

//TODO: Added the service contracts locally and then added reference using that
//TODO: Used the "context" service
namespace Core.DomainServices.FKOrganization
{
    public class FKOrganizationUnitImportService : IFKOrganizationUnitImportService
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ILogger _logger;
        private readonly string _certificateThumbprint;
        private readonly string _serviceRoot;
        private readonly string _orgUnitServiceRoot;

        public FKOrganizationUnitImportService(IOrganizationRepository organizationRepository, StsOrganisationIntegrationConfiguration configuration, ILogger logger)
        {
            _organizationRepository = organizationRepository;
            _logger = logger;
            _certificateThumbprint = configuration.CertificateThumbprint;
            _serviceRoot = $"https://{configuration.EndpointHost}/service/Organisation/Organisation/5";
            _orgUnitServiceRoot = $"https://{configuration.EndpointHost}/service/Organisation/OrganisationEnhed/5";
        }

        public Result<FKOrganizationUnit, OperationError> ImportOrganizationTree(int organizationId)
        {
            var org = _organizationRepository.GetById(organizationId);
            if (org.IsNone)
                return new OperationError(OperationFailure.NotFound);

            //TODO: Check for valid cvr

            using var clientCertificate = GetClientCertificate(_certificateThumbprint);
            using var organisationPortTypeClient = FkOrganisationEnhedHelper.CreateOrganisationPortTypeClient(CreateHttpBinding(), _orgUnitServiceRoot, clientCertificate);
            using var organizationPortTypeClient = FkOrganisationHelper.CreateOrganisationPortTypeClient(CreateHttpBinding(), _serviceRoot, clientCertificate);

            //TODO: Man kan søge med cvr hvis ikke fkorg_uuid er stemplet på endnu
            var laesOrg = FkOrganisationHelper.CreateOrganisationEnhedLaesRequest(org.Value.Cvr);
            var orgChannel = organizationPortTypeClient.ChannelFactory.CreateChannel();
            var fkOrgInstance = orgChannel.laes(laesOrg);
            var mainOrgUnitUuid = fkOrgInstance.LaesResponse1.LaesOutput.FiltreretOejebliksbillede.Registrering[0].RelationListe.Overordnet.ReferenceID.Item;


            var channel = organisationPortTypeClient.ChannelFactory.CreateChannel();

            const int pageSize = 100;
            var totalIds = new List<string>();
            var totalResults = new List<RegistreringType1>();
            var currentPage = new List<string>();
            do
            {
                currentPage.Clear();
                var enhedSoeg = FkOrganisationEnhedHelper.CreateOrganisationEnhedSoegRequest(org.Value.Cvr, org.Value.Uuid.ToString("D"), pageSize, totalIds.Count);
                var soegResponse = channel.soeg(enhedSoeg);
                //TODO: check errors
                currentPage = soegResponse.SoegResponse1.SoegOutput.IdListe.ToList();
                totalIds.AddRange(currentPage);

                var listRequest = FkOrganisationEnhedHelper.CreateOrganisationEnhedListRequest(org.Value.Cvr,currentPage.ToArray());
                var listResponse = channel.list(listRequest);
                var units = listResponse.ListResponse1.ListOutput.FiltreretOejebliksbillede.SelectMany(x=>x.Registrering);
                totalResults.AddRange(units);
            } while (currentPage.Count == pageSize);


            return new OperationError("TODO", OperationFailure.NotFound);//TODO

        }

        //TODO: A common helper also used by sts bruger
        private static X509Certificate2 GetClientCertificate(string thumbprint)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

            store.Open(OpenFlags.ReadOnly);
            X509Certificate2 result;
            try
            {
                var results = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (results.Count == 0)
                {
                    throw new Exception("Unable to find certificate!");
                }
                result = results[0];
            }
            finally
            {
                store.Close();
            }
            return result;
        }

        //TODO:Also a common helper
        private static BasicHttpBinding CreateHttpBinding()
        {
            return new BasicHttpBinding
            {
                Security =
                {
                    Mode = BasicHttpSecurityMode.Transport,
                    Transport = {ClientCredentialType = HttpClientCredentialType.Certificate}
                },
                MaxReceivedMessageSize = int.MaxValue,
                OpenTimeout = new TimeSpan(0, 3, 0),
                CloseTimeout = new TimeSpan(0, 3, 0),
                ReceiveTimeout = new TimeSpan(0, 3, 0),
                SendTimeout = new TimeSpan(0, 3, 0)
            };
        }

        //TODO: The import straegy must be injected and that must hide the soap binding
    }
}
