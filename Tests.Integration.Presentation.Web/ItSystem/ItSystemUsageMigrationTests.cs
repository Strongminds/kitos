﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Presentation.Web.Models;
using Presentation.Web.Models.ItSystemUsageMigration;
using Tests.Integration.Presentation.Web.Tools;
using Xunit;

namespace Tests.Integration.Presentation.Web.ItSystem
{
    public class ItSystemUsageMigrationTests : IAsyncLifetime
    {
        private ItSystemDTO _oldSystemInUse;
        private ItSystemUsageDTO _oldSystemUsage;
        private ItSystemDTO _newSystem;

        public async Task InitializeAsync()
        {
            _oldSystemInUse = await CreateSystemAsync();
            _oldSystemUsage = await TakeSystemIntoUseAsync(_oldSystemInUse);
            _newSystem = await CreateSystemAsync();
        }

        public Task DisposeAsync()
        {
            //Nothing to clean up
            return Task.CompletedTask;
        }

        [Theory]
        [InlineData(OrganizationRole.GlobalAdmin)]
        [InlineData(OrganizationRole.User)]
        public async Task GetUnusedItSystems_Can_Get_Specific_Unused_It_System(OrganizationRole role)
        {
            //Arrange
            var itSystemName = CreateName();
            var itSystem = await CreateSystemAsync(name: itSystemName, accessModifier: AccessModifier.Public);

            //Act
            using (var httpResponse = await GetUnusedSystemsAsync(role, TestEnvironment.DefaultOrganizationId, itSystemName, 10, true))
            {
                var response = await httpResponse.ReadResponseBodyAsKitosApiResponseAsync<IEnumerable<ItSystemSimpleDTO>>();
                //Assert
                Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                var itSystems = response.ToList();
                Assert.Single(itSystems);
                Assert.Equal(itSystemName, itSystems[0].Name);
                Assert.Equal(itSystem.Id, itSystems[0].Id);
            }
        }

        [Theory]
        [InlineData(OrganizationRole.GlobalAdmin)]
        [InlineData(OrganizationRole.User)]
        public async Task GetUnusedItSystems_Can_Limit_How_Many_Systems_To_Return(OrganizationRole role)
        {
            //Arrange
            var prefix = CreateName();
            var itSystemName1 = prefix + CreateName();
            var itSystemName2 = prefix + CreateName();
            var itSystemName3 = prefix + CreateName();
            var itSystem1 = await CreateSystemAsync(name: itSystemName1);
            var itSystem2 = await CreateSystemAsync(name: itSystemName2);
            var itSystem3 = await CreateSystemAsync(name: itSystemName3);
            var createdSystemsIds = new[] { itSystem1.Id, itSystem2.Id, itSystem3.Id };

            //Act
            using (var httpResponse = await GetUnusedSystemsAsync(role, TestEnvironment.DefaultOrganizationId, prefix, 2, true))
            {
                var response = await httpResponse.ReadResponseBodyAsKitosApiResponseAsync<IEnumerable<ItSystemSimpleDTO>>();
                //Assert
                Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                var itSystems = response.ToList();
                Assert.Equal(2, itSystems.Count);
                Assert.True(itSystems.All(x => createdSystemsIds.Contains(x.Id)));
            }
        }

        [Theory]
        [InlineData(OrganizationRole.GlobalAdmin)]
        [InlineData(OrganizationRole.User)]
        public async Task GetUnusedItSystems_Can_Include_Public_It_Systems_From_Other_Organizations_And_All_From_Own(OrganizationRole role)
        {
            //Arrange
            var prefix = CreateName();
            var itSystemName1 = prefix + CreateName();
            var itSystemName2 = prefix + CreateName();
            var itSystemName3 = prefix + CreateName();
            var ownLocalSystem = await CreateSystemAsync(name: itSystemName1);
            var sharedSystemFromOtherOrg = await CreateSystemAsync(TestEnvironment.SecondOrganizationId, itSystemName2, AccessModifier.Public);
            await CreateSystemAsync(TestEnvironment.SecondOrganizationId, itSystemName3); //Private system in other org

            //Act
            using (var httpResponse = await GetUnusedSystemsAsync(role, TestEnvironment.DefaultOrganizationId, prefix, 3, true))
            {
                var response = await httpResponse.ReadResponseBodyAsKitosApiResponseAsync<IEnumerable<ItSystemSimpleDTO>>();
                //Assert
                Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                var itSystemIds = response.Select(x => x.Id).ToList();
                Assert.Equal(2, itSystemIds.Count);
                Assert.Contains(ownLocalSystem.Id, itSystemIds);
                Assert.Contains(sharedSystemFromOtherOrg.Id, itSystemIds);
            }
        }

        [Theory]
        [InlineData(OrganizationRole.GlobalAdmin)]
        public async Task GetUnusedItSystems_Can_Include_It_Systems_From_Own_Organization_Only(OrganizationRole role)
        {
            //Arrange
            var prefix = CreateName();
            var itSystemName1 = prefix + CreateName();
            var itSystemName2 = prefix + CreateName();
            var ownLocalSystem = await CreateSystemAsync(name: itSystemName1);
            await CreateSystemAsync(TestEnvironment.SecondOrganizationId, itSystemName2, AccessModifier.Public); //shared system should not be returned when not asked to

            //Act
            using (var httpResponse = await GetUnusedSystemsAsync(role, TestEnvironment.DefaultOrganizationId, prefix, 2, false))
            {
                var response = await httpResponse.ReadResponseBodyAsKitosApiResponseAsync<IEnumerable<ItSystemSimpleDTO>>();
                //Assert
                Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                var itSystemIds = response.Select(x => x.Id).ToList();
                Assert.Single(itSystemIds);
                Assert.Contains(ownLocalSystem.Id, itSystemIds);
            }
        }

        [Fact, Description("Systems in use in our own organization should not be included")]
        public async Task GetUnusedItSystems_Does_Not_Include_Systems_In_Use()
        {
            //Arrange
            var prefix = CreateName();
            var itSystemName1 = prefix + CreateName();
            var itSystemName2 = prefix + CreateName();
            var itSystemName3 = prefix + CreateName();
            var itSystem1 = await CreateSystemAsync(name: itSystemName1);

            //These two - one local and one shared in other org - should not be returned since they are in use in own organization
            var itSystem2 = await CreateSystemAsync(name: itSystemName2);
            var itSystem3 = await CreateSystemAsync(name: itSystemName3, organizationId: TestEnvironment.SecondOrganizationId, accessModifier: AccessModifier.Public);
            await TakeSystemIntoUseAsync(itSystem2, organizationId: TestEnvironment.DefaultOrganizationId);
            await TakeSystemIntoUseAsync(itSystem3, organizationId: TestEnvironment.DefaultOrganizationId);

            //Act
            using (var httpResponse = await GetUnusedSystemsAsync(OrganizationRole.GlobalAdmin, TestEnvironment.DefaultOrganizationId, prefix, 3, true))
            {
                var response = await httpResponse.ReadResponseBodyAsKitosApiResponseAsync<IEnumerable<ItSystemSimpleDTO>>();
                //Assert
                Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                var itSystems = response.ToList();
                var dto = Assert.Single(itSystems);
                Assert.Equal(itSystem1.Id, dto.Id);
            }
        }

        [Fact, Description("Systems in use applies to current org only, do not expect results to be omitted if it system is used in another organization.")]
        public async Task GetUnusedItSystems_Does_Not_Include_Filter_Systems_In_Use_In_Other_Orgs()
        {
            //Arrange
            var prefix = CreateName();
            var itSystemName1 = prefix + CreateName();
            var itSystemName2 = prefix + CreateName();
            var itSystemName3 = prefix + CreateName();
            var itSystem1 = await CreateSystemAsync(name: itSystemName1);

            //These two - one local and one shared in other org - should not be returned since they are in use in own organization
            var itSystem2 = await CreateSystemAsync(name: itSystemName2);
            var itSystem3 = await CreateSystemAsync(name: itSystemName3, organizationId: TestEnvironment.SecondOrganizationId, accessModifier: AccessModifier.Public);
            await TakeSystemIntoUseAsync(itSystem2, organizationId: TestEnvironment.SecondOrganizationId);
            await TakeSystemIntoUseAsync(itSystem3, organizationId: TestEnvironment.SecondOrganizationId);
            var createdSystemsIds = new[] { itSystem1.Id, itSystem2.Id, itSystem3.Id };

            //Act
            using (var httpResponse = await GetUnusedSystemsAsync(OrganizationRole.GlobalAdmin, TestEnvironment.DefaultOrganizationId, prefix, 3, true))
            {
                var response = await httpResponse.ReadResponseBodyAsKitosApiResponseAsync<IEnumerable<ItSystemSimpleDTO>>();
                //Assert
                Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                var itSystems = response.ToList();
                Assert.Equal(3, itSystems.Count);
                Assert.True(itSystems.All(x => createdSystemsIds.Contains(x.Id)));
            }
        }

        [Theory]
        [InlineData(OrganizationRole.User, false)]
        [InlineData(OrganizationRole.LocalAdmin, false)]
        [InlineData(OrganizationRole.GlobalAdmin, true)]
        public async Task GetAccessibilityLevel_Returns(OrganizationRole role, bool expectedMigrationAvailability)
        {
            //Arrange
            var url = TestEnvironment.CreateUrl("api/v1/ItSystemUsageMigration/Accessibility");
            var cookie = await HttpApi.GetCookieAsync(role);

            //Act
            using (var response = await HttpApi.GetWithCookieAsync(url, cookie))
            {
                //Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var result = await response.ReadResponseBodyAsKitosApiResponseAsync<ItSystemUsageMigrationAccessDTO>();
                Assert.Equal(expectedMigrationAvailability, result.CanExecuteMigration);
            }
        }

        [Fact]
        public async Task GetMigration_When_System_Is_Not_Used_In_Contracts_Or_Projects()
        {
            //Arrange
            var usage = _oldSystemUsage;
            var newSystem = _newSystem;

            //Act
            using (var response = await GetMigration(usage, newSystem))
            {
                //Assert
                var result = await AssertMigrationReturned(response);
                Assert.Empty(result.AffectedContracts);
                Assert.Empty(result.AffectedItProjects);
                AssertFromToSystemInfo(usage, result, _oldSystemInUse, newSystem);
            }
        }

        [Fact]
        public async Task GetMigration_When_System_Is_Used_In_A_Project()
        {
            //Arrange
            var project = await ItProjectHelper.CreateProject(CreateName(), TestEnvironment.DefaultOrganizationId);
            await ItProjectHelper.AddSystemBinding(project.Id, _oldSystemUsage.Id, TestEnvironment.DefaultOrganizationId);

            //Act
            using (var response = await GetMigration(_oldSystemUsage, _newSystem))
            {
                //Assert
                var result = await AssertMigrationReturned(response);
                Assert.Empty(result.AffectedContracts);
                Assert.Equal(1, result.AffectedItProjects?.Count());
                Assert.Equal(project.Id, result.AffectedItProjects?.FirstOrDefault()?.Id);
            }
        }

        [Fact]
        public async Task GetMigration_When_System_Is_Associated_In_Contract()
        {
            //Arrange
            var contract = await CreateContractAsync();
            await ItContractHelper.AddItSystemUsage(contract.Id, _oldSystemUsage.Id, TestEnvironment.DefaultOrganizationId);

            //Act
            using (var response = await GetMigration(_oldSystemUsage, _newSystem))
            {
                //Assert
                var result = await AssertMigrationReturned(response);
                Assert.Empty(result.AffectedItProjects);
                Assert.Equal(1, result.AffectedContracts?.Count());

                var dto = result.AffectedContracts?.FirstOrDefault();
                Assert.Equal(contract.Id, dto?.Contract?.Id);
                Assert.Empty(dto.AffectedInterfaceUsages);
                Assert.Empty(dto.InterfaceExhibitUsagesToBeDeleted);
                Assert.True(dto.SystemAssociatedInContract);
            }
        }

        [Fact]
        public async Task GetMigration_When_System_Is_Associated_Through_UseInterface_Mappings_In_Contract()
        {
            //Arrange
            var createdInterface = await CreateInterfaceAsync();
            var contract = await CreateContractAsync();
            var usage = await CreateInterfaceUsageAsync(contract, createdInterface, _oldSystemUsage, _oldSystemInUse);

            //Adding an unaffected usage (not same system usage source)
            var unaffectedItSystem = await CreateSystemAsync();
            var unaffectedUsage = await TakeSystemIntoUseAsync(unaffectedItSystem);
            await CreateInterfaceUsageAsync(contract, createdInterface, unaffectedUsage, unaffectedItSystem);

            //Act
            using (var response = await GetMigration(_oldSystemUsage, _newSystem))
            {
                //Assert
                var result = await AssertMigrationReturned(response);
                Assert.Empty(result.AffectedItProjects);
                Assert.Equal(1, result.AffectedContracts?.Count());
                var migrationDto = result.AffectedContracts?.First();
                Assert.Empty(migrationDto.InterfaceExhibitUsagesToBeDeleted);
                Assert.Equal(1, migrationDto.AffectedInterfaceUsages?.Count());
                AssertInterfaceMapping(usage, migrationDto.AffectedInterfaceUsages?.First());
            }
        }

        [Fact]
        public async Task GetMigration_When_System_Is_Associated_Through_InterfaceExhibit_Mappings_In_Contract()
        {
            //Arrange
            var createdInterface = await CreateInterfaceAsync();
            var contract = await CreateContractAsync();
            var exhibit = await CreateExhibitAsync(createdInterface, _oldSystemInUse);
            var exhibitUsage = await CreateExhibitUsageAsync(contract, exhibit, _oldSystemUsage);

            //Adding an unaffected exhibit usage (not same system usage source)
            var unaffectedItSystem = await CreateSystemAsync();
            var unaffectedInterface = await CreateInterfaceAsync();
            var unaffectedUsage = await TakeSystemIntoUseAsync(unaffectedItSystem);
            var unAffectedExhibit = await CreateExhibitAsync(unaffectedInterface, unaffectedItSystem);
            await CreateExhibitUsageAsync(contract, unAffectedExhibit, unaffectedUsage);

            //Act
            using (var response = await GetMigration(_oldSystemUsage, _newSystem))
            {
                //Assert
                var result = await AssertMigrationReturned(response);
                Assert.Empty(result.AffectedItProjects);
                Assert.Equal(1, result.AffectedContracts?.Count());
                var migrationDto = result.AffectedContracts?.First();
                Assert.Empty(migrationDto.AffectedInterfaceUsages);
                Assert.Equal(1, migrationDto.InterfaceExhibitUsagesToBeDeleted?.Count());
                AssertInterfaceMapping(exhibitUsage, migrationDto.InterfaceExhibitUsagesToBeDeleted?.First());
            }
        }

        private static async Task<HttpResponseMessage> GetUnusedSystemsAsync(OrganizationRole role, int organizationId, string nameContent, int take, bool getPublic)
        {
            var token = await HttpApi.GetTokenAsync(role);
            var url = TestEnvironment.CreateUrl($"api/v1/ItSystemUsageMigration/UnusedItSystems" +
                                                $"?organizationId={organizationId}" +
                                                $"&nameContent={nameContent}" +
                                                $"&numberOfItSystems={take}" +
                                                $"&getPublicFromOtherOrganizations={getPublic}");

            return await HttpApi.GetWithTokenAsync(url, token.Token);
        }

        private static async Task<HttpResponseMessage> GetMigration(ItSystemUsageDTO usage, ItSystemDTO toSystem)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var url = TestEnvironment.CreateUrl($"api/v1/ItSystemUsageMigration?usageId={usage.Id}&toSystemId={toSystem.Id}");

            return await HttpApi.GetWithCookieAsync(url, cookie);
        }

        private static void AssertInterfaceMapping(ItInterfaceUsageDTO createdBinding, NamedEntityDTO affectedUsage)
        {
            Assert.Equal(createdBinding.ItInterfaceId, affectedUsage.Id);
            Assert.Equal(createdBinding.ItInterfaceItInterfaceName, affectedUsage.Name);
        }

        private static void AssertInterfaceMapping(ItInterfaceExhibitUsageDTO createdBinding, NamedEntityDTO affectedUsage)
        {
            Assert.Equal(createdBinding.ItInterfaceExhibitItInterfaceId, affectedUsage.Id);
            Assert.Equal(createdBinding.ItInterfaceExhibitItInterfaceName, affectedUsage.Name);
        }


        private static async Task<ItSystemUsageMigrationDTO> AssertMigrationReturned(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.ReadResponseBodyAsKitosApiResponseAsync<ItSystemUsageMigrationDTO>();
            Assert.NotNull(result);
            return result;
        }

        private static void AssertFromToSystemInfo(
            ItSystemUsageDTO usage,
            ItSystemUsageMigrationDTO result,
            ItSystemDTO oldSystem,
            ItSystemDTO newSystem)
        {
            Assert.Equal(usage.Id, result.TargetUsage.Id);
            Assert.Equal(oldSystem.Id, result.FromSystem.Id);
            Assert.Equal(newSystem.Id, result.ToSystem.Id);
        }

        private static Task<ItSystemDTO> CreateSystemAsync(
            int organizationId = TestEnvironment.DefaultOrganizationId,
            string name = null,
            AccessModifier accessModifier = AccessModifier.Local)
        {
            return ItSystemHelper.CreateItSystemInOrganizationAsync(name ?? CreateName(), organizationId, accessModifier);
        }

        private static async Task<ItInterfaceExhibitDTO> CreateExhibitAsync(ItInterfaceDTO exposedInterface, ItSystemDTO exposingSystem)
        {
            return await InterfaceExhibitHelper.CreateExhibit(exposingSystem.Id, exposedInterface.Id);
        }

        private static async Task<ItSystemUsageDTO> TakeSystemIntoUseAsync(ItSystemDTO system, int? organizationId = null)
        {
            return await ItSystemHelper.TakeIntoUseAsync(system.Id, organizationId ?? system.OrganizationId);
        }

        private static async Task<ItContractDTO> CreateContractAsync()
        {
            return await ItContractHelper.CreateContract(CreateName(), TestEnvironment.DefaultOrganizationId);
        }

        private static async Task<ItInterfaceDTO> CreateInterfaceAsync()
        {
            var createdInterface = await InterfaceHelper.CreateInterface(InterfaceHelper.CreateInterfaceDto(CreateName(),
                CreateName(), TestEnvironment.DefaultUserId, TestEnvironment.DefaultOrganizationId, AccessModifier.Public));
            return createdInterface;
        }

        private static async Task<ItInterfaceUsageDTO> CreateInterfaceUsageAsync(ItContractDTO contract, ItInterfaceDTO targetInterface, ItSystemUsageDTO usage, ItSystemDTO system)
        {
            return await InterfaceUsageHelper.CreateAsync(contract.Id, usage.Id, system.Id, targetInterface.Id, TestEnvironment.DefaultOrganizationId);
        }

        private static async Task<ItInterfaceExhibitUsageDTO> CreateExhibitUsageAsync(ItContractDTO contract, ItInterfaceExhibitDTO exhibit, ItSystemUsageDTO systemUsage)
        {
            return await InterfaceExhibitHelper.CreateExhibitUsage(contract.Id, systemUsage.Id, exhibit.Id);
        }

        private static string CreateName()
        {
            return $"{Guid.NewGuid():N}";
        }
    }
}
