﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.DomainModel;
using Core.DomainModel.ItSystem;
using Presentation.Web.Models.SystemRelations;
using Tests.Integration.Presentation.Web.Tools;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Integration.Presentation.Web.ItSystem
{
    public class SystemRelationsTest : WithAutoFixture
    {
        private const int OrganizationId = TestEnvironment.DefaultOrganizationId;

        [Fact]
        public async Task Post_SystemRelation_Returns_201()
        {
            //Arrange
            var input = await PrepareFullRelationAsync();

            //Act
            using (var response = await ItSystemHelper.SendPostRelationAsync(input))
            {
                //Assert
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                var relations = (await ItSystemHelper.GetRelationsAsync(input.SourceUsageId)).ToList();
                Assert.Equal(1, relations.Count);
                var dto = relations.Single();
                Assert.Equal(input.SourceUsageId, dto.Source.Id);
                Assert.Equal(input.TargetUsageId, dto.Destination.Id);
                Assert.Equal(input.Description, dto.Description);
                Assert.Equal(input.Reference, dto.Reference);
                Assert.Equal(input.ContractId.Value, dto.Contract.Id);
                Assert.Equal(input.InterfaceId.Value, dto.Interface.Id);
                Assert.Equal(input.FrequencyTypeId.Value, dto.FrequencyType.Id);
            }
        }

        [Fact]
        public async Task Delete_SystemRelation_Returns_204()
        {
            //Arrange
            var system1 = await ItSystemHelper.CreateItSystemInOrganizationAsync(CreateName(), OrganizationId, AccessModifier.Public);
            var system2 = await ItSystemHelper.CreateItSystemInOrganizationAsync(CreateName(), OrganizationId, AccessModifier.Public);
            var usage1 = await ItSystemHelper.TakeIntoUseAsync(system1.Id, OrganizationId);
            var usage2 = await ItSystemHelper.TakeIntoUseAsync(system2.Id, OrganizationId);

            var input = new CreateSystemRelationDTO
            {
                SourceUsageId = usage1.Id,
                TargetUsageId = usage2.Id,
                Description = A<string>(),
                Reference = A<string>(),
            };

            using (var response = await ItSystemHelper.SendPostRelationAsync(input))
            {
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                var createdRelation = await response.ReadResponseBodyAsKitosApiResponseAsync<SystemRelationDTO>();

                //Act
                using (var deleteResponse = await ItSystemHelper.SendDeleteRelationAsync(usage1.Id, createdRelation.Id))
                {
                    //Assert
                    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
                }
            }
        }

        [Fact]
        public async Task Can_Delete_SystemUsageWithRelations()
        {
            //Arrange
            var input = await PrepareFullRelationAsync();

            using (await ItSystemHelper.SendPostRelationAsync(input))
            using (var deletionResponse = await ItSystemHelper.SendRemoveUsageAsync(input.SourceUsageId, OrganizationId))
            using (var getAfterDeleteResponse = await ItSystemHelper.SendGetRelationAsync(input.SourceUsageId, OrganizationId))
            {
                Assert.Equal(HttpStatusCode.OK, deletionResponse.StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
            }
        }

        private async Task<CreateSystemRelationDTO> PrepareFullRelationAsync()
        {
            var system1 = await ItSystemHelper.CreateItSystemInOrganizationAsync(CreateName(), OrganizationId, AccessModifier.Public);
            var system2 = await ItSystemHelper.CreateItSystemInOrganizationAsync(CreateName(), OrganizationId, AccessModifier.Public);
            var usage1 = await ItSystemHelper.TakeIntoUseAsync(system1.Id, OrganizationId);
            var usage2 = await ItSystemHelper.TakeIntoUseAsync(system2.Id, OrganizationId);
            var targetInterface = await InterfaceHelper.CreateInterface(
                InterfaceHelper.CreateInterfaceDto(CreateName(), CreateName(), null, OrganizationId, AccessModifier.Public));
            await InterfaceExhibitHelper.CreateExhibit(system2.Id, targetInterface.Id);
            var contract = await ItContractHelper.CreateContract(CreateName(), OrganizationId);

            var targetFrequencyTypeId = DatabaseAccess.MapFromEntitySet<RelationFrequencyType, int>(repo =>
            {
                var first = repo
                    .AsQueryable()
                    .First(x => x.IsEnabled);
                return first.Id;
            });

            var input = new CreateSystemRelationDTO
            {
                SourceUsageId = usage1.Id,
                TargetUsageId = usage2.Id,
                ContractId = contract.Id,
                InterfaceId = targetInterface.Id,
                Description = A<string>(),
                Reference = A<string>(),
                FrequencyTypeId = targetFrequencyTypeId
            };
            return input;
        }

        //TODO: Test that it system can be deleted without any nasty bindings

        private string CreateName()
        {
            return $"Relations_{A<Guid>():N}";
        }
    }
}
