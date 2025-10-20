using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V2.Request.DataProcessing;
using Presentation.Web.Models.API.V2.Types.DataProcessing;
using System.Linq;
using System.Threading.Tasks;
using Presentation.Web.Models.API.V2.Request.System.Regular;
using Presentation.Web.Models.API.V2.Request.SystemUsage;
using Presentation.Web.Models.API.V2.Types.Shared;
using Tests.Integration.Presentation.Web.Tools;
using Tests.Integration.Presentation.Web.Tools.External;
using Tests.Integration.Presentation.Web.Tools.Internal.Organizations;
using Xunit;
using OrganizationType = Presentation.Web.Models.API.V2.Types.Organization.OrganizationType;

namespace Tests.Integration.Presentation.Web.Organizations.V2
{
    public class SupplierApiV2Test : OrganizationApiV2TestBase
    {
        [Fact]
        public async Task Can_Perform_Local_Admin_Suppliers_Flow()
        {
            var organization = await CreateOrganizationAsync();
            var supplier = await CreateOrganizationAsync(type: OrganizationType.Company, isSupplier: true);
            var supplier2 = await CreateOrganizationAsync(type: OrganizationType.Company, isSupplier: true);

            await OrganizationSupplierInternalV2Helper.AddSupplier(organization.Uuid, supplier.Uuid);

            var response = await OrganizationSupplierInternalV2Helper.GetSuppliers(organization.Uuid);

            var responseSupplier = Assert.Single(response);
            Assert.Equal(supplier.Uuid, responseSupplier.Uuid);
            Assert.Equal(supplier.Name, responseSupplier.Name);
            Assert.Equal(supplier.Cvr, responseSupplier.Cvr);

            var availableSuppliersResponse =
                await OrganizationSupplierInternalV2Helper.GetAvailableSuppliers(organization.Uuid);

            var supplierUuidsList = availableSuppliersResponse.Select(x => x.Uuid).ToList();

            Assert.DoesNotContain(supplier.Uuid, supplierUuidsList);
            Assert.Contains(supplier2.Uuid, supplierUuidsList);

            await OrganizationSupplierInternalV2Helper.DeleteSupplier(organization.Uuid, supplier.Uuid);

            var responseAfterDelete = await OrganizationSupplierInternalV2Helper.GetSuppliers(organization.Uuid);
            Assert.Empty(responseAfterDelete);
        }
        
        [Fact]
        public async Task Supplier_Can_Only_Update_Supplier_Fields()
        {
            var organization = await CreateOrganizationAsync();
            var supplier = await CreateOrganizationAsync(type: OrganizationType.Company, isSupplier: true);
            var (_, _, token) = await HttpApi.CreateUserAndGetToken(CreateEmail(), OrganizationRole.User, supplier.Uuid, true);
            var globalAdminToken = await GetGlobalToken();

            await OrganizationSupplierInternalV2Helper.AddSupplier(organization.Uuid, supplier.Uuid);

            var dpr = await DataProcessingRegistrationV2Helper.PostAsync(globalAdminToken,
                new CreateDataProcessingRegistrationRequestDTO { Name = A<string>(),
                    OrganizationUuid = organization.Uuid
                });
            var system = await ItSystemV2Helper.CreateSystemAsync(globalAdminToken, new CreateItSystemRequestDTO
            {
                Name = A<string>(),
                OrganizationUuid = organization.Uuid
            });
            var usage = await ItSystemUsageV2Helper.PostAsync(globalAdminToken,
                new CreateItSystemUsageRequestDTO { OrganizationUuid = organization.Uuid, SystemUuid = system.Uuid });

            var oversightDateRequest = A<ModifyOversightDateDTO>();
            var postResponse = await DataProcessingRegistrationV2Helper.PostOversightDate(dpr.Uuid, oversightDateRequest, token);
            AssertOversightDate(oversightDateRequest, postResponse);

            var updateUsageRequest = new GeneralDataUpdateRequestDTO
            {
                ContainsAITechnology = A<YesNoUndecidedChoice>()
            };
            using var updatedUsage = await ItSystemUsageV2Helper.SendPatchGeneral(token, usage.Uuid, updateUsageRequest);
            Assert.False(updatedUsage.IsSuccessStatusCode);

            var patchRequest = A<ModifyOversightDateDTO>();
            var patchResponse = await DataProcessingRegistrationV2Helper.PatchOversightDate(dpr.Uuid, postResponse.Uuid, patchRequest, token);
            AssertOversightDate(patchRequest, patchResponse);

            await DataProcessingRegistrationV2Helper.DeleteOversightDate(dpr.Uuid, postResponse.Uuid, token);
            var dprAfterOperations = await DataProcessingRegistrationV2Helper.GetDPRAsync(globalAdminToken, dpr.Uuid);

            Assert.Empty(dprAfterOperations.Oversight.OversightDates);
        }
        
        [Fact]
        public async Task Supplier_Cannot_Update_Non_Supplier_Fields()
        {
            var organization = await CreateOrganizationAsync();
            var supplier = await CreateOrganizationAsync(type: OrganizationType.Company, isSupplier: true);
            var (_, _, token) = await HttpApi.CreateUserAndGetToken(CreateEmail(), OrganizationRole.User, supplier.Uuid, true);
            var globalAdminToken = await GetGlobalToken();

            await OrganizationSupplierInternalV2Helper.AddSupplier(organization.Uuid, supplier.Uuid);

            var system = await ItSystemV2Helper.CreateSystemAsync(globalAdminToken, new CreateItSystemRequestDTO
            {
                Name = A<string>(),
                OrganizationUuid = organization.Uuid
            });
            var usage = await ItSystemUsageV2Helper.PostAsync(globalAdminToken,
                new CreateItSystemUsageRequestDTO { OrganizationUuid = organization.Uuid, SystemUuid = system.Uuid });

            var dpr = await DataProcessingRegistrationV2Helper.PostAsync(globalAdminToken,
                new CreateDataProcessingRegistrationRequestDTO { Name = A<string>(),
                    OrganizationUuid = organization.Uuid
                });

            var dprUpdate = A<DataProcessingRegistrationGeneralDataWriteRequestDTO>();
            using var failedResponse = await DataProcessingRegistrationV2Helper.SendPatchGeneralDataAsync(token, dpr.Uuid, dprUpdate);
            Assert.False(failedResponse.IsSuccessStatusCode);

            var updateUsageRequest = A<GeneralDataUpdateRequestDTO>();
            using var updatedUsage = await ItSystemUsageV2Helper.SendPatchGeneral(token, usage.Uuid, updateUsageRequest);
            Assert.False(updatedUsage.IsSuccessStatusCode);
        }
        
        [Fact]
        public async Task Supplier_Cannot_Update_Fields_With_No_Supplier_And_No_Access()
        {
            var organization = await CreateOrganizationAsync();
            var supplier = await CreateOrganizationAsync(type: OrganizationType.Company, isSupplier: true);
            var (_, _, token) = await HttpApi.CreateUserAndGetToken(CreateEmail(), OrganizationRole.User, supplier.Uuid, true);
            var globalAdminToken = await GetGlobalToken();

            var dpr = await DataProcessingRegistrationV2Helper.PostAsync(globalAdminToken,
                new CreateDataProcessingRegistrationRequestDTO { Name = A<string>(),
                    OrganizationUuid = organization.Uuid
                });

            var system = await ItSystemV2Helper.CreateSystemAsync(globalAdminToken, new CreateItSystemRequestDTO
            {
                Name = A<string>(),
                OrganizationUuid = organization.Uuid
            });
            var usage = await ItSystemUsageV2Helper.PostAsync(globalAdminToken,
                new CreateItSystemUsageRequestDTO { OrganizationUuid = organization.Uuid, SystemUuid = system.Uuid });

            var oversightDateRequest = A<ModifyOversightDateDTO>();
            using var postResponse = await DataProcessingRegistrationV2Helper.SendPostOversightDate(dpr.Uuid, oversightDateRequest, token);
            Assert.False(postResponse.IsSuccessStatusCode);

            var dprUpdate = A<DataProcessingRegistrationGeneralDataWriteRequestDTO>();
            using var failedResponse = await DataProcessingRegistrationV2Helper.SendPatchGeneralDataAsync(token, dpr.Uuid, dprUpdate);
            Assert.False(failedResponse.IsSuccessStatusCode);

            var updateUsageRequest = A<GeneralDataUpdateRequestDTO>();
            using var updatedUsage = await ItSystemUsageV2Helper.SendPatchGeneral(token, usage.Uuid, updateUsageRequest);
            Assert.False(updatedUsage.IsSuccessStatusCode);
        }

        [Fact]
        public async Task LocalAdmin_Cannot_Update_Supplier_Fields()
        {
            var organization = await CreateOrganizationAsync();
            var supplier = await CreateOrganizationAsync(type: OrganizationType.Company, isSupplier: true);
            var (_, _, token) = await HttpApi.CreateUserAndGetToken(CreateEmail(), OrganizationRole.LocalAdmin, organization.Uuid, true);
            var globalAdminToken = await GetGlobalToken();

            await OrganizationSupplierInternalV2Helper.AddSupplier(organization.Uuid, supplier.Uuid);

            var dpr = await DataProcessingRegistrationV2Helper.PostAsync(token,
                new CreateDataProcessingRegistrationRequestDTO { Name = A<string>(),
                    OrganizationUuid = organization.Uuid
                });

            var oversightDateRequest = A<ModifyOversightDateDTO>();
            using var failedPostResponse = await DataProcessingRegistrationV2Helper.SendPostOversightDate(dpr.Uuid, oversightDateRequest, token);
            Assert.False(failedPostResponse.IsSuccessStatusCode);

            var postResponse = await DataProcessingRegistrationV2Helper.PostOversightDate(dpr.Uuid, oversightDateRequest, globalAdminToken);
            
            var patchRequest = A<ModifyOversightDateDTO>();
            using var failedPatchResponse = await DataProcessingRegistrationV2Helper.SendPatchOversightDate(dpr.Uuid, postResponse.Uuid, patchRequest, token);
            Assert.False(failedPatchResponse.IsSuccessStatusCode);

            using var failedDeleteResponse = await DataProcessingRegistrationV2Helper.SendDeleteOversightDate(dpr.Uuid, postResponse.Uuid, token);
            Assert.False(failedDeleteResponse.IsSuccessStatusCode);
        }

        private static void AssertOversightDate(ModifyOversightDateDTO expected, OversightDateDTO actual)
        {
            Assert.Equal(expected.Remark, actual.Remark);
            Assert.Equal(expected.CompletedAt, actual.CompletedAt);
            Assert.Equal(expected.OversightReportLink.Name, actual.OversightReportLink.Name);
            Assert.Equal(expected.OversightReportLink.Url, actual.OversightReportLink.Url);
        }
    }
}
