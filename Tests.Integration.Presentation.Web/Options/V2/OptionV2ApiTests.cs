using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V2.Internal.Request;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using Presentation.Web.Models.API.V2.Internal.Response.GlobalOptions;
using Tests.Integration.Presentation.Web.Tools;
using Tests.Integration.Presentation.Web.Tools.External;
using Tests.Integration.Presentation.Web.Tools.Internal;
using Tests.Integration.Presentation.Web.Tools.XUnit;
using Xunit;

namespace Tests.Integration.Presentation.Web.Options.V2
{
    [Collection(nameof(SequentialTestGroup))]
    public class OptionV2ApiTests : BaseTest
    {
        [Theory, MemberData(nameof(GetV2ResourceNames))]
        public async Task Can_Get_AvailableOptions(string apiv2OptionResource)
        {
            //Arrange
            var orgUuid = DatabaseAccess.GetEntityUuid<Organization>(TestEnvironment.DefaultOrganizationId);
            var pageSize = Math.Max(1, A<int>() % 2); //Minimum is 1;
            var pageNumber = 0; //Always takes the first page;

            //Act
            var optionTypes = (await OptionV2ApiHelper.GetOptionsAsync(apiv2OptionResource, orgUuid, pageSize, pageNumber)).ToList();

            //Assert
            Assert.Equal(pageSize, optionTypes.Count);
        }

        [Theory, MemberData(nameof(GetV2OptionResourceNames))]
        public async Task Can_Get_Specific_Option_That_Is_Available(string apiv2OptionResource, string apiV2GlobalOptionName, string apiV2GlobalOptionPrefix)
        {
            //Arrange
            var name = A<string>();
            var description = A<string>();
            await CreateAndActivateOption(name, description, apiV2GlobalOptionName, apiV2GlobalOptionPrefix);
            var organizationDto = await OrganizationV2Helper.GetOrganizationAsync(await GetGlobalToken(), DefaultOrgUuid);
            var organizationUuid = organizationDto.Uuid;
            var options = await OptionV2ApiHelper.GetOptionsAsync(apiv2OptionResource, organizationUuid, 100, 0); //100 should be more than enough to get all.
            var option = options.First(x => x.Name.Equals(name)); //Get the newly created type.

            //Act
            var result = await OptionV2ApiHelper.GetOptionAsync(apiv2OptionResource, option.Uuid, organizationUuid);

            //Assert
            Assert.Equal(name, result.Name);
            Assert.Equal(description, result.Description);
            Assert.Equal(option.Uuid, result.Uuid);
            Assert.True(result.IsAvailable);
        }

        [Theory, MemberData(nameof(GetV2OptionResourceNames))]
        public async Task Can_Get_Specific_Option_That_Is_Not_Available(string apiv2OptionResource, string apiV2GlobalOptionName, string apiV2GlobalOptionPrefix)
        {
            //Arrange
            var orgId = TestEnvironment.DefaultOrganizationId;
            var newName = A<string>();
            var createdType =
                await CreateAndActivateOption(newName, A<string>(), apiV2GlobalOptionName, apiV2GlobalOptionPrefix);
            var organizationDto =
                await OrganizationV2Helper.GetOrganizationAsync(await GetGlobalToken(), DefaultOrgUuid);
            var organizationUuid = organizationDto.Uuid;
            var options = await OptionV2ApiHelper.GetOptionsAsync(apiv2OptionResource, organizationUuid, 100, 0); //100 should be more than enough to get all.
            var option = options.First(x => x.Name.Equals(newName)); //Get the newly created type.

            //Disable the option
            var dto = A<GlobalRegularOptionUpdateRequestDTO>();
            dto.IsObligatory = false;
            dto.IsEnabled = true;
            dto.Name = createdType.Name;
            await GlobalOptionTypeV2Helper.PatchGlobalOptionType(createdType.Uuid, apiV2GlobalOptionName, dto, apiV2GlobalOptionPrefix).WithExpectedResponseCode(HttpStatusCode.OK).DisposeAsync();

            //Act
            var result = await OptionV2ApiHelper.GetOptionAsync(apiv2OptionResource, option.Uuid, organizationUuid);

            //Assert
            Assert.Equal(option.Name, result.Name);
            Assert.Equal(option.Uuid, result.Uuid);
            Assert.False(result.IsAvailable);
        }

        [Theory, MemberData(nameof(GetRoleResources))]
        public async Task Can_Get_WriteAccess_Status_For_Role_Options(string apiv2OptionResource, string apiV2GlobalOptionName, string apiV2GlobalOptionPrefix)
        {
            //Arrange
            var writeAccess = A<bool>();
            var orgId = TestEnvironment.DefaultOrganizationId;
            var name = A<string>();
            await CreateAndActivateRoleOption(name, A<string>(), writeAccess, apiV2GlobalOptionName, apiV2GlobalOptionPrefix);
            var organizationDto = await OrganizationV2Helper.GetOrganizationAsync(await GetGlobalToken(), DefaultOrgUuid);
            var organizationUuid = organizationDto.Uuid;
            var options = await OptionV2ApiHelper.GetOptionsAsync(apiv2OptionResource, organizationUuid, 100, 0); //100 should be more than enough to get all.
            var option = options.First(x => x.Name.Equals(name)); //Get the newly created type.

            //Act
            var result = await OptionV2ApiHelper.GetRoleOptionAsync(apiv2OptionResource, option.Uuid, organizationUuid);

            //Assert
            Assert.Equal(name, result.Name);
            Assert.Equal(option.Uuid, result.Uuid);
            Assert.True(result.IsAvailable);
            Assert.Equal(writeAccess, result.WriteAccess);
        }

        private async Task<GlobalRegularOptionResponseDTO> CreateAndActivateOption(string optionName, string description, string optionTypeName, string optionPrefix)
        {
            using var newOptionResponse = await GlobalOptionTypeV2Helper.CreateGlobalOptionType(optionTypeName,
                new GlobalRegularOptionCreateRequestDTO
                {
                    Name = optionName,
                    Description = description,
                    IsObligatory = true,
                }, optionPrefix);
            var optionType = await newOptionResponse.ReadResponseBodyAsAsync<GlobalRegularOptionResponseDTO>();
            var activateResponse = await GlobalOptionTypeV2Helper.PatchGlobalOptionType(optionType.Uuid, optionTypeName,
                new GlobalRegularOptionUpdateRequestDTO
                {
                    Name = optionName,
                    Description = description,
                    IsObligatory = true,
                    IsEnabled = true
                }, optionPrefix);
            return await activateResponse.ReadResponseBodyAsAsync<GlobalRegularOptionResponseDTO>();
        }

        private async Task CreateAndActivateRoleOption(string optionName, string description, bool writeAccess, string optionTypeName, string optionPrefix)
        {
            using var newOptionResponse = await GlobalOptionTypeV2Helper.CreateGlobalRoleOptionType(optionTypeName,
                new GlobalRoleOptionCreateRequestDTO
                {
                    Name = optionName,
                    Description = description,
                    WriteAccess = writeAccess,
                    IsObligatory = true,
                }, optionPrefix);
            var optionType = await newOptionResponse.ReadResponseBodyAsAsync<GlobalRegularOptionResponseDTO>();
            await GlobalOptionTypeV2Helper.PatchGlobalRoleOptionType(optionType.Uuid, optionTypeName,
                new GlobalRoleOptionUpdateRequestDTO()
                {
                    Name = optionName,
                    Description = description,
                    WriteAccess = writeAccess,
                    IsObligatory = true,
                    IsEnabled = true
                }, optionPrefix);
        }

        public static IEnumerable<object[]> GetV2ResourceNames()
        {
            foreach (var v2ResourceName in GetV2OptionResourceNames())
            {
                yield return new[] { v2ResourceName[0] };
            }
        }

        public static IEnumerable<object[]> GetV2OptionResourceNames()
        {
            return GetRegularResources().Concat(GetRoleResources());
        }

        public static IEnumerable<object[]> GetRoleResources()
        {
            yield return new[] { OptionV2ApiHelper.ResourceName.ItSystemUsageRoles, GlobalOptionTypeV2Helper.ItSystemRoles, GlobalOptionTypeV2Helper.ItSystemPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.DataProcessingRegistrationRoles, GlobalOptionTypeV2Helper.DprRoles, GlobalOptionTypeV2Helper.DataProcessingPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItContractRoles, GlobalOptionTypeV2Helper.ItContractRoles, GlobalOptionTypeV2Helper.ItContractPrefix };
        }

        public static IEnumerable<object[]> GetRegularResources()
        {
            yield return new[] { OptionV2ApiHelper.ResourceName.BusinessType, GlobalOptionTypeV2Helper.BusinessTypes, GlobalOptionTypeV2Helper.ItSystemPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItSystemUsageDataClassification, GlobalOptionTypeV2Helper.ItSystemCategoriesTypes, GlobalOptionTypeV2Helper.ItSystemPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItSystemUsageRelationFrequencies, GlobalOptionTypeV2Helper.FrequencyRelationTypes, GlobalOptionTypeV2Helper.ItSystemPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItSystemUsageArchiveTypes, GlobalOptionTypeV2Helper.ArchiveTypes, GlobalOptionTypeV2Helper.ItSystemPrefix };

            yield return new[] { OptionV2ApiHelper.ResourceName.ItSystemUsageArchiveTestLocations, GlobalOptionTypeV2Helper.ArchiveTestLocationTypes, GlobalOptionTypeV2Helper.ItSystemPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItSystemSensitivePersonalDataTypes, GlobalOptionTypeV2Helper.SensitivePersonalDataTypes, GlobalOptionTypeV2Helper.ItSystemPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItSystemUsageRegisterTypes, GlobalOptionTypeV2Helper.LocalRegisterTypes, GlobalOptionTypeV2Helper.ItSystemPrefix };

            yield return new[] { OptionV2ApiHelper.ResourceName.DataProcessingRegistrationDataResponsible, GlobalOptionTypeV2Helper.DataResponsibleTypes, GlobalOptionTypeV2Helper.DataProcessingPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.DataProcessingRegistrationBasisForTransfer, GlobalOptionTypeV2Helper.BasisForTransferTypes, GlobalOptionTypeV2Helper.DataProcessingPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.DataProcessingRegistrationCountry, GlobalOptionTypeV2Helper.CountryOptionTypes, GlobalOptionTypeV2Helper.DataProcessingPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.DataProcessingRegistrationOversight, GlobalOptionTypeV2Helper.OversightOptionTypes, GlobalOptionTypeV2Helper.DataProcessingPrefix };

            yield return new[] { OptionV2ApiHelper.ResourceName.ItContractContractTypes, GlobalOptionTypeV2Helper.ItContractTypes, GlobalOptionTypeV2Helper.ItContractPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItContractContractTemplateTypes, GlobalOptionTypeV2Helper.TemplateTypes, GlobalOptionTypeV2Helper.ItContractPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItContractPurchaseTypes, GlobalOptionTypeV2Helper.PurchaseFormTypes, GlobalOptionTypeV2Helper.ItContractPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItContractPaymentModelTypes, GlobalOptionTypeV2Helper.PaymentModelTypes, GlobalOptionTypeV2Helper.ItContractPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItContractAgreementElementTypes, GlobalOptionTypeV2Helper.AgreementElementTypes, GlobalOptionTypeV2Helper.ItContractPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItContractPaymentFrequencyTypes, GlobalOptionTypeV2Helper.PaymentFrequencyTypes, GlobalOptionTypeV2Helper.ItContractPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItContractPriceRegulationTypes, GlobalOptionTypeV2Helper.PriceRegulationTypes, GlobalOptionTypeV2Helper.ItContractPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItContractProcurementStrategyTypes, GlobalOptionTypeV2Helper.ProcurementStrategyTypes, GlobalOptionTypeV2Helper.ItContractPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItContractAgreementExtensionOptionTypes, GlobalOptionTypeV2Helper.OptionExtendTypes, GlobalOptionTypeV2Helper.ItContractPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItContractNoticePeriodMonthTypes, GlobalOptionTypeV2Helper.TerminationDeadlineTypes, GlobalOptionTypeV2Helper.ItContractPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.CriticalityTypes, GlobalOptionTypeV2Helper.CriticalityTypes, GlobalOptionTypeV2Helper.ItContractPrefix };

            yield return new[] { OptionV2ApiHelper.ResourceName.ItInterfaceTypes, GlobalOptionTypeV2Helper.InterfaceTypes, GlobalOptionTypeV2Helper.ItSystemPrefix };
            yield return new[] { OptionV2ApiHelper.ResourceName.ItInterfaceDataTypes, GlobalOptionTypeV2Helper.DataTypes, GlobalOptionTypeV2Helper.ItSystemPrefix };

        }
    }
}
