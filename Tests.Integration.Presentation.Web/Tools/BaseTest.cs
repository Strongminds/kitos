using System;
using System.Threading.Tasks;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V2.Internal.Request.Organizations;
using Presentation.Web.Models.API.V2.Request.Contract;
using Presentation.Web.Models.API.V2.Request.DataProcessing;
using Presentation.Web.Models.API.V2.Request.OrganizationUnit;
using Presentation.Web.Models.API.V2.Request.System.Regular;
using Presentation.Web.Models.API.V2.Request.SystemUsage;
using Presentation.Web.Models.API.V2.Response.Contract;
using Presentation.Web.Models.API.V2.Response.DataProcessing;
using Presentation.Web.Models.API.V2.Response.Organization;
using Presentation.Web.Models.API.V2.Response.System;
using Presentation.Web.Models.API.V2.Response.SystemUsage;
using Presentation.Web.Models.API.V2.Types.Shared;
using Tests.Integration.Presentation.Web.Tools.External;
using Tests.Integration.Presentation.Web.Tools.Internal.Organizations;
using Tests.Toolkit.Patterns;
using OrganizationType = Presentation.Web.Models.API.V2.Types.Organization.OrganizationType;

namespace Tests.Integration.Presentation.Web.Tools
{
    public class BaseTest : WithAutoFixture
    {
        private string _token;
        public readonly Guid DefaultOrgGuid = DatabaseAccess.GetEntityUuid<Organization>(TestEnvironment.DefaultOrganizationId);

        public async Task<ShallowOrganizationResponseDTO> CreateOrganizationAsync(string name = null, string cvr = null)
        {
            var defaultRequest = new OrganizationCreateRequestDTO
            {
                Name = name ?? A<string>(),
                Cvr = cvr ?? CreateCvr(),
                Type = A<OrganizationType>()
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

        public async Task<ItContractResponseDTO> CreateItContractAsync(Guid organizationUuid, string name = null)
        {
            return await ItContractV2Helper.PostContractAsync(await GetGlobalToken(), new CreateNewContractRequestDTO
            {
                OrganizationUuid = organizationUuid,
                Name = name ?? A<string>()
            });
        }

        public async Task<ItSystemResponseDTO> CreateItSystemAsync(Guid orgGuid, RegistrationScopeChoice scope = RegistrationScopeChoice.Global, string name = null)
        {
            var request = new CreateItSystemRequestDTO
            {
                Name = name ?? A<string>(),
                OrganizationUuid = orgGuid,
                Scope = scope
            };
            return await ItSystemV2Helper.CreateSystemAsync(await GetGlobalToken(), request);
        }

        public async Task<ItSystemUsageResponseDTO> TakeSystemIntoUsageAsync(Guid systemUuid, Guid orgUuid)
        {
            var request = new CreateItSystemUsageRequestDTO
            {
                SystemUuid = systemUuid,
                OrganizationUuid = orgUuid
            };
            return await ItSystemUsageV2Helper.PostAsync(await GetGlobalToken(), request);
        }

        public async Task<OrganizationUnitResponseDTO> CreateOrganizationUnitAsync(Guid organizationUuid, string name = null, Guid? parentUnitUuid = null)
        {
            var rootUnit = await OrganizationUnitV2Helper.GetRootUnit(organizationUuid);
            var request = new CreateOrganizationUnitRequestDTO
            {
                Name = name ?? A<string>(),
                ParentUuid = parentUnitUuid ?? rootUnit.Uuid
            };
            return await OrganizationUnitV2Helper.CreateUnitAsync(organizationUuid, request);
        }

        public string CreateCvr()
        {
            var rnd = new Random();
            
            return rnd.Next(0, 100_000_000)
                .ToString("D8");
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

        //Temp. method to strangle out old helpers
        public int GetOrgId(Guid orgUuid)
        {
            return DatabaseAccess.GetEntityId<Organization>(orgUuid);
        }

        public Guid DefaultOrganizationUuid()
        {
            return DatabaseAccess.GetEntityUuid<Organization>(TestEnvironment.DefaultOrganizationId);
        }
    }
}
