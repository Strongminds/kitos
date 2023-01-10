using System;
using Core.Abstractions.Types;
using Core.DomainModel.Organization;
using Core.DomainServices.Repositories.Organization;
using Core.DomainServices.SSO;
using Infrastructure.STS.Company.DomainServices;
using Infrastructure.STS.Organization.DomainServices;
using Infrastructure.STS.OrganizationUnit.DomainServices;
using Moq;
using Serilog;
using Xunit;

namespace Tests.Unit.Presentation.Web.Authorization
{
    public class StsOrgTest
    {
        class repoStuvb : IStsOrganizationIdentityRepository
        {
            public Maybe<StsOrganizationIdentity> GetByExternalUuid(Guid externalId)
            {
                return Maybe<StsOrganizationIdentity>.None;
            }

            public Result<StsOrganizationIdentity, OperationError> AddNew(Organization organization, Guid externalId)
            {
                return new StsOrganizationIdentity(externalId, organization);
            }
        }

        [Fact]
        public void TestMiddelfart()
        {
            var configuration = new StsOrganisationIntegrationConfiguration("44d05370a7bcfb9fa9958341e2171e89de26d6a1", "prod.serviceplatformen.dk");
            var stsOrgService = new StsOrganizationService(configuration,
                new StsOrganizationCompanyLookupService(configuration, Mock.Of<ILogger>()),
                new repoStuvb(), Mock.Of<ILogger>());

            var service = new StsOrganizationUnitService(stsOrgService, configuration, Mock.Of<ILogger>());


            var result = service.ResolveOrganizationTree(new Organization() { Cvr = "29189684" });
        }
    }
}
