using System;
using System.Threading.Tasks;
using AutoFixture;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V2.Internal.Request.Organizations;
using Presentation.Web.Models.API.V2.Request.DataProcessing;
using Presentation.Web.Models.API.V2.Request.OrganizationUnit;
using Presentation.Web.Models.API.V2.Response.DataProcessing;
using Presentation.Web.Models.API.V2.Response.Organization;
using Tests.Integration.Presentation.Web.Tools.External;
using Tests.Integration.Presentation.Web.Tools.Internal.Organizations;
using Tests.Toolkit.Patterns;
using OrganizationType = Presentation.Web.Models.API.V2.Types.Organization.OrganizationType;

namespace Tests.Integration.Presentation.Web.Tools
{
    public class BaseTest : WithAutoFixture
    {
        private string _token;

        public async Task<OrganizationResponseDTO> CreateOrganizationAsync()
        {
            var defaultRequest = new OrganizationCreateRequestDTO
            {
                Name = A<string>(),
                Cvr = CreateCvr(),
                Type = OrganizationType.Municipality
            };
            using var response = OrganizationInternalV2Helper.CreateOrganization(defaultRequest);
            return await response;
        }

        public async Task<DataProcessingRegistrationResponseDTO> CreateDPRAsync(Guid orgUuid, string name = null)
        {
            var token = await GetGlobalToken();
            var request = new CreateDataProcessingRegistrationRequestDTO
            {
                Name = name ?? A<string>(),
                OrganizationUuid = orgUuid,
            };
            return await DataProcessingRegistrationV2Helper.PostAsync(token, request);
        }

        public string CreateCvr()
        {
            var fixture = new Fixture();

            fixture.Customizations.Add(new RandomNumericSequenceGenerator(8));

            var random8Digits = fixture.Create<string>();

            return random8Digits;
        }

        public async Task<string> GetGlobalToken()
        {
            if (_token != null)
            {
                return _token;
            }

            var token = await HttpApi.GetTokenAsync(OrganizationRole.GlobalAdmin);
            _token = token.Token;
            return _token;
        }
    }
}
