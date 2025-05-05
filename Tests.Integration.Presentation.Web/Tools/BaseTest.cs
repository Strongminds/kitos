using System;
using System.Threading.Tasks;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V2.Request.DataProcessing;
using Presentation.Web.Models.API.V2.Response.DataProcessing;
using Tests.Integration.Presentation.Web.Tools.External;
using Tests.Toolkit.Patterns;

namespace Tests.Integration.Presentation.Web.Tools
{
    public class BaseTest : WithAutoFixture
    {
        private string _token;

        public async Task<DataProcessingRegistrationResponseDTO> CreateDPRAsync(Guid orgUuid, string name = null)
        {
            var token = await GetToken();
            var request = new CreateDataProcessingRegistrationRequestDTO
            {
                Name = name ?? A<string>(),
                OrganizationUuid = orgUuid,
            };
            return await DataProcessingRegistrationV2Helper.PostAsync(token, request);
        }

        private async Task<string> GetToken()
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
