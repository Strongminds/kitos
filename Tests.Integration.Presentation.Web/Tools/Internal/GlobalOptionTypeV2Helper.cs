

using Core.DomainModel.Organization;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Presentation.Web.Models.API.V2.Internal.Request.Options;

namespace Tests.Integration.Presentation.Web.Tools.Internal
{
    public class GlobalOptionTypeV2Helper
    {
        //Prefix types
        public const string ItSystemPrefix = "it-systems";
        public const string DataProcessingPrefix = "dpr";
        public const string ItContractPrefix = "it-contract";
        public const string OrganizationUnitPrefix = "organizations";

        //It System types
        public const string ItSystemRoles = "it-systems-roles";
        public const string BusinessTypes = "business-types";
        public const string ArchiveTypes = "archive-types";
        public const string ArchiveLocationTypes = "archive-location-types";
        public const string ArchiveTestLocationTypes = "archive-test-location-types";
        public const string SensitivePersonalDataTypes = "sensitive-personal-data-types";
        public const string LocalRegisterTypes = "register-types";
        public const string ItSystemCategoriesTypes = "it-system-categories";
        public const string DataTypes = "data-types";
        public const string FrequencyRelationTypes = "frequency-types";
        public const string InterfaceTypes = "interface-types";

        //It contract types
        public const string ItContractRoles = "it-contract-roles";
        public const string ItContractTypes = "it-contract-types";
        public const string TemplateTypes = "it-contract-template-types";
        public const string PurchaseFormTypes = "purchase-form-types";
        public const string PaymentModelTypes = "payment-model-types";
        public const string AgreementElementTypes = "agreement-element-types";
        public const string OptionExtendTypes = "option-extend-types";
        public const string PaymentFrequencyTypes = "payment-frequency-types";
        public const string PriceRegulationTypes = "price-regulation-types";
        public const string ProcurementStrategyTypes = "procurement-strategy-types";
        public const string TerminationDeadlineTypes = "termination-deadline-types";
        public const string CriticalityTypes = "criticality-types";

        //Data processing types
        public const string DprRoles = "dpr-roles";
        public const string BasisForTransferTypes = "data-processing-basis-for-transfer-options";
        public const string OversightOptionTypes = "data-processing-oversight-options";
        public const string DataResponsibleTypes = "data-processing-data-responsible-options";
        public const string CountryOptionTypes = "data-processing-country-options";

        //Organization unit types
        public const string OrganizationUnitRoles = "organization-unit-roles";

        private const string GlobalOptionTypesSuffix = "global-option-types";

        public static async Task<HttpResponseMessage> GetGlobalOptionTypes(string choiceTypeName, string apiPrefix)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            return await HttpApi.GetWithCookieAsync(
                TestEnvironment.CreateUrl($"{apiPrefix}/{GlobalOptionTypesSuffix}/{choiceTypeName}"), cookie);
        }

        public static async Task<HttpResponseMessage> CreateGlobalOptionType(string choiceTypeName, GlobalRegularOptionCreateRequestDTO dto, string apiPrefix)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            return await HttpApi.PostWithCookieAsync(
                TestEnvironment.CreateUrl($"{apiPrefix}/{GlobalOptionTypesSuffix}/{choiceTypeName}"), cookie, dto);
        }

        public static async Task<HttpResponseMessage> PatchGlobalOptionType(Guid optionUuid, string choiceTypeName, GlobalRegularOptionUpdateRequestDTO dto, string apiPrefix)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            return await HttpApi.PatchWithCookieAsync(
                TestEnvironment.CreateUrl($"api/v2/internal/{apiPrefix}/{GlobalOptionTypesSuffix}/{choiceTypeName}/{optionUuid}"), cookie, dto);
        }
    }
}
