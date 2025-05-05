using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel.GDPR.Read;
using Core.DomainModel.Organization;
using Core.DomainModel.Shared;
using Presentation.Web.Models.API.V1;
using Presentation.Web.Models.API.V1.GDPR;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools
{
    public static class DataProcessingRegistrationHelper
    {
        public static async Task<DataProcessingRegistrationDTO> CreateAsync(int organizationId, string name, Cookie optionalLogin = null)
        {
            using var createdResponse = await SendCreateRequestAsync(organizationId, name, optionalLogin);
            if (!createdResponse.IsSuccessStatusCode)
                throw new Exception($"ERROR creating DPR:{name} in org with id:{organizationId}. Error code: {createdResponse.StatusCode} Message:{await createdResponse.Content.ReadAsStringAsync()}");

            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            var response = await createdResponse.ReadResponseBodyAsKitosApiResponseAsync<DataProcessingRegistrationDTO>();

            Assert.Equal(name, response.Name);

            return response;
        }

        public static async Task<HttpResponseMessage> SendCreateRequestAsync(int organizationId, string name, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var body = new CreateDataProcessingRegistrationDTO
            {
                Name = name,
                OrganizationId = organizationId
            };

            return await HttpApi.PostWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration"), cookie, body);
        }

        public static async Task<HttpResponseMessage> SendDeleteRequestAsync(int id, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            return await HttpApi.DeleteWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}"), cookie);
        }

        public static async Task<HttpResponseMessage> SendChangeNameRequestAsync(int id, string name, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var body = new SingleValueDTO<string> { Value = name };

            return await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}/name"), cookie, body);
        }

        public static async Task<IEnumerable<DataProcessingRegistrationReadModel>> QueryReadModelByNameContent(int organizationId, string nameContent, int top, int skip, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl($"odata/Organizations({organizationId})/DataProcessingRegistrationReadModels?$expand=RoleAssignments&$filter=contains(Name,'{nameContent}')&$top={top}&$skip={skip}&$orderBy=Name"), cookie);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadOdataListResponseBodyAsAsync<DataProcessingRegistrationReadModel>();
        }

        public static async Task<IEnumerable<BusinessRoleDTO>> GetAvailableRolesAsync(int id, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}/available-roles"), cookie);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadResponseBodyAsKitosApiResponseAsync<IEnumerable<BusinessRoleDTO>>();
        }
        
        public static async Task<IEnumerable<UserWithEmailDTO>> GetAvailableUsersAsync(int id, int roleId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}/available-roles/{roleId}/applicable-users"), cookie);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadResponseBodyAsKitosApiResponseAsync<IEnumerable<UserWithEmailDTO>>();
        }

        public static async Task<HttpResponseMessage> SendAssignRoleRequestAsync(int id, int roleId, int userId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            return await HttpApi.PatchWithCookieAsync(
                TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}/roles/assign"), cookie,
                new AssignRoleDTO
                {
                    RoleId = roleId,
                    UserId = userId
                });
        }

        public static async Task<HttpResponseMessage> SendRemoveRoleRequestAsync(int id, int roleId, int userId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            return await HttpApi.PatchWithCookieAsync(
                TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}/roles/remove/{roleId}/from/{userId}"), cookie,
                new { });
        }

        public static async Task<HttpResponseMessage> SendAssignDataProcessorRequestAsync(int registrationId, int organizationId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            return await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{registrationId}/data-processors/assign"), cookie, new SingleValueDTO<int> { Value = organizationId });
        }

        public static async Task<HttpResponseMessage> SendAssignSubDataProcessorRequestAsync(int registrationId, int organizationId, Cookie optionalLogin = null, SubDataProcessorDetailsDTO details = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            return await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{registrationId}/sub-data-processors/assign"), cookie, new AssignSubDataProcessorRequestDTO()
            {
                OrganizationId = organizationId,
                Details = details
            });
        }

        public static async Task<HttpResponseMessage> SendSetUseSubDataProcessorsStateRequestAsync(int registrationId, YesNoUndecidedOption value, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            return await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{registrationId}/sub-data-processors/state"), cookie, new SingleValueDTO<YesNoUndecidedOption> { Value = value });
        }

        public static async Task<HttpResponseMessage> SendChangeIsAgreementConcludedRequestAsync(int id, YesNoIrrelevantOption? yesNoIrrelevantOption, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var body = new SingleValueDTO<YesNoIrrelevantOption?> { Value = yesNoIrrelevantOption };

            return await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}/agreement-concluded"), cookie, body);
        }

        public static async Task<HttpResponseMessage> SendChangeAgreementConcludedAtRequestAsync(int id, DateTime? dateTime, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var body = new SingleValueDTO<DateTime?> { Value = dateTime };

            return await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}/agreement-concluded-at"), cookie, body);
        }

        public static async Task<HttpResponseMessage> SendSetUseTransferToInsecureThirdCountriesStateRequestAsync(int registrationId, YesNoUndecidedOption value, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            return await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{registrationId}/insecure-third-countries/state"), cookie, new SingleValueDTO<YesNoUndecidedOption> { Value = value });
        }

        public static async Task<HttpResponseMessage> SendAssignBasisForTransferRequestAsync(int registrationId, int basisForTransferId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            return await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{registrationId}/basis-for-transfer/assign"), cookie, new SingleValueDTO<int> { Value = basisForTransferId });
        }

        public static async Task<DataProcessingOptionsDTO> GetAvailableOptionsRequestAsync(int organizationId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var response = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/available-options-in/{organizationId}"), cookie);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadResponseBodyAsKitosApiResponseAsync<DataProcessingOptionsDTO>();
        }

        public static async Task<HttpResponseMessage> SendAssignDataResponsibleRequestAsync(int id, int? dataResponsibleOptionId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var body = new SingleValueDTO<int?> { Value = dataResponsibleOptionId };

            return await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}/data-responsible/assign"), cookie, body);
        }

        public static async Task<HttpResponseMessage> SendAssignOversightOptionRequestAsync(int id, int oversightOptionId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var body = new SingleValueDTO<int> { Value = oversightOptionId };

            return await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}/oversight-option/assign"), cookie, body);
        }

        public static async Task<HttpResponseMessage> SendUpdateMainContractRequestAsync(int id, int contractId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var body = new SingleValueDTO<int> { Value = contractId };

            return await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/data-processing-registration/{id}/main-contract/update"), cookie, body);
        }

    }
}
