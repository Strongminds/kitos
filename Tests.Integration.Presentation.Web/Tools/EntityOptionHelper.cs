﻿using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V1;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools
{
    public static class EntityOptionHelper
    {
        public static class ResourceNames
        {
            public const string BusinessType = "BusinessTypes";
            public const string ItSystemCategories = "ItSystemCategories";
            public const string FrequencyTypes = "FrequencyTypes";
            public const string ArchiveTypes = "ArchiveTypes";
            public const string ArchiveLocations = "ArchiveLocations";
            public const string ArchiveTestLocations = "ArchiveTestLocations";
            public const string RegisterTypes = "RegisterTypes";
            public const string SensitivePersonalDataTypes = "SensitivePersonalDataTypes";
            public const string SystemRoles = "ItSystemRoles";

            public const string ContractTypes = "ItContractTypes";
            public const string ContractTemplateTypes = "ItContractTemplateTypes";
            public const string PurchaseTypes = "PurchaseFormTypes";
            public const string PaymentModelTypes = "PaymentModelTypes";
            public const string AgreementElementTypes = "AgreementElementTypes";
            public const string PaymentFrequencyTypes = "PaymentFrequencyTypes";
            public const string PriceRegulationTypes = "PriceRegulationTypes";
            public const string ContractRoles = "ItContractRoles";

            public const string DataProcessingDataResponsibleOptions = "DataProcessingDataResponsibleOptions";
            public const string DataProcessingBasisForTransferOptions = "DataProcessingBasisForTransferOptions";
            public const string DataProcessingCountryOptions = "DataProcessingCountryOptions";
            public const string DataProcessingOversightOptions = "DataProcessingOversightOptions";
            public const string DataProcessingRegistrationRoles = "DataProcessingRegistrationRoles";
        }

        public static async Task<OptionDTO> CreateOptionTypeAsync(string resource, string optionName, int organizationId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl($"odata/{resource}?organizationId={organizationId}");

            var body = new
            {
                IsObligatory = true,
                IsEnabled = true,
                Name = optionName
            };

            using var response = await HttpApi.PostWithCookieAsync(url, cookie, body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.ReadResponseBodyAsAsync<OptionDTO>();
        }

        public static async Task<RoleDTO> CreateRoleOptionTypeAsync(string resource, string optionName, int organizationId, bool writeAccess = false,Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl($"odata/{resource}?organizationId={organizationId}");

            var body = new
            {
                IsObligatory = true,
                IsEnabled = true,
                Name = optionName,
                HasWriteAccess = writeAccess
            };

            using var response = await HttpApi.PostWithCookieAsync(url, cookie, body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.ReadResponseBodyAsAsync<RoleDTO>();
        }

        public static async Task<HttpResponseMessage> ChangeOptionTypeNameAsync(string resource, int id, string name, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl($"odata/{resource}({id})");

            var body = new
            {
                Name = name
            };

            using var response = await HttpApi.PatchWithCookieAsync(url, cookie, body);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            return response;
        }

        public static async Task<HttpResponseMessage> SendChangeOptionIsObligatoryAsync(string resource, int id, bool isObligatory, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl($"odata/{resource}({id})");

            var body = new
            {
                IsObligatory = isObligatory
            };

            using var response = await HttpApi.PatchWithCookieAsync(url, cookie, body);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            return response;
        }
    }
}
