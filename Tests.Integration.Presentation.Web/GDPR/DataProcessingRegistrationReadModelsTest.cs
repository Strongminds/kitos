using Core.DomainModel;
using Core.DomainModel.Organization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Abstractions.Extensions;
using Core.DomainModel.GDPR;
using Core.DomainModel.Shared;
using Presentation.Web.Models.API.V2.Request.DataProcessing;
using Presentation.Web.Models.API.V2.Types.DataProcessing;
using Presentation.Web.Models.API.V2.Types.Shared;
using Tests.Integration.Presentation.Web.Tools;
using Tests.Integration.Presentation.Web.Tools.External;
using Xunit;
using Tests.Integration.Presentation.Web.Tools.XUnit;
using Xunit.Abstractions;
using Presentation.Web.Controllers.API.V2.External.DataProcessingRegistrations.Mapping;
using Presentation.Web.Controllers.API.V2.External.Generic;
using Presentation.Web.Models.API.V1.GDPR;
using Presentation.Web.Models.API.V2.Response.Generic.Identity;

namespace Tests.Integration.Presentation.Web.GDPR
{
    [Collection(nameof(SequentialTestGroup))]
    public class DataProcessingRegistrationReadModelsTest : BaseTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public DataProcessingRegistrationReadModelsTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Can_Query_And_Page_ReadModels()
        {
            //Arrange
            var organizationId = TestEnvironment.DefaultOrganizationId;
            var suffix = A<Guid>().ToString("N");
            var name1 = $"1_{suffix}";
            var name2 = $"2_{suffix}";
            var name3 = $"3_{suffix}";

            await DataProcessingRegistrationHelper.CreateAsync(organizationId, name1);
            await DataProcessingRegistrationHelper.CreateAsync(organizationId, name2);
            await DataProcessingRegistrationHelper.CreateAsync(organizationId, name3);

            //Act
            var page1 = (await DataProcessingRegistrationHelper.QueryReadModelByNameContent(organizationId, suffix, 2, 0)).ToList();
            var page2 = (await DataProcessingRegistrationHelper.QueryReadModelByNameContent(organizationId, suffix, 2, 2)).ToList();

            //Assert
            Assert.Equal(2, page1.Count);
            Assert.Equal(name1, page1.First().Name);
            Assert.Equal(name2, page1.Last().Name);

            Assert.Single(page2);
            Assert.Equal(name3, page2.Single().Name);
        }

        [Fact]
        public async Task ReadModels_Contain_Correct_Content()
        {
            //Arrange
            var name = A<string>();
            var dpName = $"Dp:{name}";
            var subDpName = $"Sub_Dp:{name}";
            var systemName = $"SYSTEM:{name}";
            var contractName = $"CONTRACT:{name}";
            var refName = $"REF:{name}";
            var refUserAssignedId = $"REF:{name}EXT_ID";
            var refUrl = $"https://www.test-rm{A<uint>()}.dk";
            var organizationId = TestEnvironment.DefaultOrganizationId;
            var orgUuid = DatabaseAccess.GetEntityUuid<Organization>(organizationId);
            var isAgreementConcluded = A<YesNoIrrelevantOption>();
            var oversightInterval = A<OversightIntervalChoice>();
            var oversightCompleted = YesNoUndecidedChoice.Yes;
            var oversightDate = A<DateTime>();
            var oversightRemark = A<string>();
            var transferToThirdCountries = A<YesNoUndecidedChoice>();
            var oversightScheduledInspectionDate = A<DateTime>();

            Console.Out.WriteLine($"Testing in the context of DPR with name:{name}");

            var dataProcessor = await OrganizationHelper.CreateOrganizationAsync(organizationId, dpName, "22334455", OrganizationTypeKeys.Virksomhed, AccessModifier.Public);
            var subDataProcessor = await OrganizationHelper.CreateOrganizationAsync(organizationId, subDpName, "22314455", OrganizationTypeKeys.Virksomhed, AccessModifier.Public);
            var registration = await CreateDPRAsync(orgUuid, name);
            var regId = DatabaseAccess.GetEntityId<DataProcessingRegistration>(registration.Uuid);

            var token = await HttpApi.GetTokenAsync(OrganizationRole.GlobalAdmin);
            var oversight = new OversightDateDTO { CompletedAt = oversightDate, Remark = oversightRemark };

            var businessRoleDtos = await DataProcessingRegistrationHelper.GetAvailableRolesAsync(regId);
            var role = businessRoleDtos.First();
            var availableUsers = await DataProcessingRegistrationHelper.GetAvailableUsersAsync(regId, role.Id);
            var user = availableUsers.First();
            using var response = await DataProcessingRegistrationHelper.SendAssignRoleRequestAsync(regId, role.Id, user.Id);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            async Task<IEnumerable<IdentityNamePairResponseDTO>> OptionsFetcherHelper(string resource) => await OptionV2ApiHelper.GetOptionsAsync(resource, orgUuid, 25, 0);
            
            var basisForTransferOptions = await OptionsFetcherHelper(OptionV2ApiHelper.ResourceName.DataProcessingRegistrationBasisForTransfer);
            var dataResponsibleOptions = await OptionsFetcherHelper(OptionV2ApiHelper.ResourceName.DataProcessingRegistrationDataResponsible);
            var oversightOptions = await OptionsFetcherHelper(OptionV2ApiHelper.ResourceName.DataProcessingRegistrationOversight);
            var basisForTransfer = basisForTransferOptions.First();
            var dataResponsibleOption = dataResponsibleOptions.First();
            var oversightOption = oversightOptions.First();

            var subDataProcessorRequest = new DataProcessorRegistrationSubDataProcessorWriteRequestDTO
            {
                DataProcessorOrganizationUuid = subDataProcessor.Uuid
            };

            using var generalResponse = await DataProcessingRegistrationV2Helper.SendPatchGeneralDataAsync(
                token.Token,
                registration.Uuid, new DataProcessingRegistrationGeneralDataWriteRequestDTO
                {
                    BasisForTransferUuid = basisForTransfer.Uuid,
                    DataResponsibleUuid = dataResponsibleOption.Uuid,
                    TransferToInsecureThirdCountries = transferToThirdCountries,
                    HasSubDataProcessors = YesNoUndecidedChoice.Yes,
                    SubDataProcessors = subDataProcessorRequest.WrapAsEnumerable(),
                    DataProcessorUuids = dataProcessor.Uuid.WrapAsEnumerable()
                });
            Assert.Equal(HttpStatusCode.OK, generalResponse.StatusCode);

            var oversightResponse = await DataProcessingRegistrationV2Helper.SendPatchOversightAsync(token.Token, registration.Uuid,
                new DataProcessingRegistrationOversightWriteRequestDTO
                {
                    OversightScheduledInspectionDate = oversightScheduledInspectionDate,
                    IsOversightCompleted = oversightCompleted,
                    OversightDates = oversight.WrapAsEnumerable(),
                    OversightInterval = oversightInterval,
                    OversightOptionUuids = oversightOption.Uuid.WrapAsEnumerable(),
                });
            Assert.Equal(HttpStatusCode.OK, oversightResponse.StatusCode);

            //Concluded state
            await DataProcessingRegistrationHelper.SendChangeIsAgreementConcludedRequestAsync(regId, isAgreementConcluded).DisposeAsync();

            //References
            await ReferencesHelper.CreateReferenceAsync(refName, refUserAssignedId, refUrl, dto => dto.DataProcessingRegistration_Id = regId);

            //Systems
            var itSystemDto = await ItSystemHelper.CreateItSystemInOrganizationAsync(systemName, organizationId, AccessModifier.Public);
            var usage = await ItSystemHelper.TakeIntoUseAsync(itSystemDto.Id, organizationId);
            using var assignSystemResponse = await DataProcessingRegistrationV2Helper.PatchSystemsAsync(registration.Uuid, usage.Uuid.WrapAsEnumerable());
            Assert.Equal(HttpStatusCode.OK, assignSystemResponse.StatusCode);

            //Contracts
            var contractDto = await ItContractHelper.CreateContract(contractName, organizationId);
            using var assignDataProcessingResponse = await ItContractHelper.SendAssignDataProcessingRegistrationAsync(contractDto.Id, regId);
            Assert.Equal(HttpStatusCode.OK, assignDataProcessingResponse.StatusCode);

            await DataProcessingRegistrationHelper.SendUpdateMainContractRequestAsync(regId, contractDto.Id).WithExpectedResponseCode(HttpStatusCode.OK).DisposeAsync();

            //Wait for read model to rebuild (wait for the LAST mutation)
            await WaitForReadModelQueueDepletion();
            Console.Out.WriteLine("Read models are up to date");

            //Act
            var readModels = (await DataProcessingRegistrationHelper.QueryReadModelByNameContent(organizationId, name, 1, 0)).ToList();

            //Assert
            var readModel = Assert.Single(readModels);
            Console.Out.WriteLine("Read model found");
            Assert.Equal(name, readModel.Name);
            Assert.Equal(regId, readModel.SourceEntityId);
            Assert.Equal(refName, readModel.MainReferenceTitle);
            Assert.Equal(refUrl, readModel.MainReferenceUrl);
            Assert.Equal(refUserAssignedId, readModel.MainReferenceUserAssignedId);
            Assert.Equal(oversightInterval.ToIntervalOption(), readModel.OversightInterval);
            Assert.Equal(oversightCompleted.ToYesNoUndecidedOption(), readModel.IsOversightCompleted);
            Assert.Equal(oversightScheduledInspectionDate, readModel.OversightScheduledInspectionDate);
            Assert.Equal(dataProcessor.Name, readModel.DataProcessorNamesAsCsv);
            Assert.Equal(subDataProcessor.Name, readModel.SubDataProcessorNamesAsCsv);
            Assert.Equal(isAgreementConcluded, readModel.IsAgreementConcluded);
            Assert.Equal(transferToThirdCountries.ToYesNoUndecidedOption(), readModel.TransferToInsecureThirdCountries);
            Assert.Equal(basisForTransfer.Name, readModel.BasisForTransfer);
            Assert.Equal(dataResponsibleOption.Name, readModel.DataResponsible);
            Assert.Equal(oversightOption.Name, readModel.OversightOptionNamesAsCsv);
            Assert.Equal(contractName, readModel.ContractNamesAsCsv);
            Assert.Equal(systemName, readModel.SystemNamesAsCsv);
            Assert.Equal(itSystemDto.Uuid.ToString(), readModel.SystemUuidsAsCsv);
            Assert.Equal(oversightDate, readModel.LatestOversightDate);
            Assert.Equal(oversightRemark, readModel.LatestOversightRemark);
            Assert.True(readModel.IsActive);
            Assert.True(readModel.ActiveAccordingToMainContract);

            Console.Out.WriteLine("Flat values asserted");
            Console.Out.WriteLine("Asserting role assignments");

            var roleAssignment = Assert.Single(readModel.RoleAssignments);
            Console.Out.WriteLine("Found one role assignment as expected");

            Assert.Equal(role.Id, roleAssignment.RoleId);
            Assert.Equal(user.Id, roleAssignment.UserId);
            Assert.Equal(user.Name, roleAssignment.UserFullName);
            Assert.Equal(user.Email, roleAssignment.Email);

            Console.Out.WriteLine("Role data verified");

            //Assert that the source object can be deleted and that the readmodel is gone now
            using var deleteResponse = await DataProcessingRegistrationHelper.SendDeleteRequestAsync(regId);
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

            readModels = (await DataProcessingRegistrationHelper.QueryReadModelByNameContent(organizationId, name, 1, 0)).ToList();
            Assert.Empty(readModels);
        }

        [Fact]
        public async Task ReadModels_MainContract_Is_Updated_When_MainContract_Is_Deleted()
        {
            //Arrange
            var dprName = A<string>();
            var contractName = A<string>();
            var organizationId = TestEnvironment.DefaultOrganizationId;
            var dpr = await DataProcessingRegistrationHelper.CreateAsync(organizationId, dprName);

            var contract = await ItContractHelper.CreateContract(contractName, organizationId);
            using var assignDprResponse = await ItContractHelper.SendAssignDataProcessingRegistrationAsync(contract.Id, dpr.Id).WithExpectedResponseCode(HttpStatusCode.OK);
            using var updateMainContractResponse = await DataProcessingRegistrationHelper.SendUpdateMainContractRequestAsync(dpr.Id, contract.Id).WithExpectedResponseCode(HttpStatusCode.OK);

            await WaitForReadModelQueueDepletion();
            await ItContractHelper.SendDeleteContractRequestAsync(contract.Id).DisposeAsync();
            await WaitForReadModelQueueDepletion();

            //Act
            var readModels = await DataProcessingRegistrationHelper.QueryReadModelByNameContent(organizationId, dprName, 1, 0);

            //Assert
            var readModel = Assert.Single(readModels);
            Assert.True(readModel.IsActive);
            Assert.True(readModel.ActiveAccordingToMainContract);
        }

        [Fact]
        public async Task ReadModels_Contain_Correct_Dependent_Content()
        {
            //Arrange
            var name = A<string>();
            var organizationId = TestEnvironment.DefaultOrganizationId;
            var isAgreementConcluded = YesNoIrrelevantOption.YES;
            var agreementConcludedAt = A<DateTime>();

            Console.Out.WriteLine($"Testing in the context of DPR with name:{name}");

            var registration = await DataProcessingRegistrationHelper.CreateAsync(organizationId, name);

            await DataProcessingRegistrationHelper.SendChangeIsAgreementConcludedRequestAsync(registration.Id, isAgreementConcluded).DisposeAsync();
            await DataProcessingRegistrationHelper.SendChangeAgreementConcludedAtRequestAsync(registration.Id, agreementConcludedAt).DisposeAsync();

            //Wait for read model to rebuild (wait for the LAST mutation)
            await WaitForReadModelQueueDepletion();
            Console.Out.WriteLine("Read models are up to date");

            //Act
            var result = (await DataProcessingRegistrationHelper.QueryReadModelByNameContent(organizationId, name, 1, 0)).ToList();

            //Assert
            var readModel = Assert.Single(result);
            Console.Out.WriteLine("Read model found");
            Assert.Equal(name, readModel.Name);
            Assert.Equal(registration.Id, readModel.SourceEntityId);

            Console.Out.WriteLine("Asserting Dependent and dependee properties");
            Assert.Equal(isAgreementConcluded, readModel.IsAgreementConcluded);
            Assert.Equal(agreementConcludedAt, readModel.AgreementConcludedAt);

        }

        [Fact]
        [Description("Tests that child entities are removed from the read model when updated from the source model")]
        public async Task ReadModels_Update_When_Child_Entities_Are_Removed()
        {
            //Arrange
            var name = A<string>();
            var organizationId = TestEnvironment.DefaultOrganizationId;

            var registration = await DataProcessingRegistrationHelper.CreateAsync(organizationId, name);
            var businessRoleDtos = await DataProcessingRegistrationHelper.GetAvailableRolesAsync(registration.Id);
            var role = businessRoleDtos.First();
            var availableUsers = await DataProcessingRegistrationHelper.GetAvailableUsersAsync(registration.Id, role.Id);
            var user = availableUsers.First();
            using var response1 = await DataProcessingRegistrationHelper.SendAssignRoleRequestAsync(registration.Id, role.Id, user.Id);
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

            await WaitForReadModelQueueDepletion();

            using var response2 = await DataProcessingRegistrationHelper.SendRemoveRoleRequestAsync(registration.Id, role.Id, user.Id);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

            await WaitForReadModelQueueDepletion();

            //Act
            var result = (await DataProcessingRegistrationHelper.QueryReadModelByNameContent(organizationId, name, 1, 0)).ToList();

            //Assert
            var readModel = Assert.Single(result);
            Assert.Equal(name, readModel.Name);
            Assert.Equal(registration.Id, readModel.SourceEntityId);
            Assert.Empty(readModel.RoleAssignments);
        }

        [Fact]
        public async Task ReadModels_LastChangedBy_Updates_OnChange()
        {
            //Arrange
            var name = A<string>();
            var newName = A<string>();
            var email = A<string>();
            var orgRole = OrganizationRole.GlobalAdmin;
            var organizationId = TestEnvironment.DefaultOrganizationId;

            var registration = await DataProcessingRegistrationHelper.CreateAsync(organizationId, name);
            var (userId, _, cookie) = await HttpApi.CreateUserAndLogin(email, orgRole);
            using var response = await DataProcessingRegistrationHelper.SendChangeNameRequestAsync(registration.Id, newName, cookie);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await WaitForReadModelQueueDepletion();

            //Act
            var result = (await DataProcessingRegistrationHelper.QueryReadModelByNameContent(organizationId, newName, 1, 0)).ToList();

            //Assert
            var readModel = Assert.Single(result);
            Assert.Equal(newName, readModel.Name);
            Assert.Equal(registration.Id, readModel.SourceEntityId);
            Assert.Equal(userId, readModel.LastChangedById);
        }
        private static async Task WaitForReadModelQueueDepletion()
        {
            await ReadModelTestTools.WaitForReadModelQueueDepletion();
        }
    }
}
