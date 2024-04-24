﻿using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Core.DomainModel.ItContract;
using Core.DomainModel.Organization;
using Core.DomainModel;
using ExpectedObjects;
using Presentation.Web.Models.API.V1;
using Tests.Integration.Presentation.Web.Tools;
using Tests.Integration.Presentation.Web.Tools.External;
using Tests.Toolkit.Patterns;
using Xunit;
using Presentation.Web.Models.API.V2.Request.Generic.Roles;
using Tests.Toolkit.Extensions;
using Core.DomainServices.Extensions;
using Presentation.Web.Models.API.V2.Request.Contract;
using Presentation.Web.Models.API.V2.Internal.Response.Roles;

namespace Tests.Integration.Presentation.Web.Contract.V2
{
    public class ItContractsInternalApiV2Test : WithAutoFixture
    {
        [Fact]
        public async Task Can_Get_Available_DataProcessingRegistrations()
        {
            //Arrange
            const int organizationId = TestEnvironment.DefaultOrganizationId;
            var registrationName = A<string>();
            var registration1 = await DataProcessingRegistrationHelper.CreateAsync(organizationId, registrationName + "1");
            var registration2 = await DataProcessingRegistrationHelper.CreateAsync(organizationId, registrationName + "2");
            var contract = await ItContractHelper.CreateContract(A<string>(), organizationId);

            //Act
            var dtos = (await ItContractV2Helper.GetAvailableDataProcessingRegistrationsAsync(contract.Uuid, registrationName)).ToList();

            //Assert
            Assert.Equal(2, dtos.Count);
            dtos.Select(x => new { x.Uuid, x.Name }).ToExpectedObject().ShouldMatch(new[] { new { registration1.Uuid, registration1.Name }, new { registration2.Uuid, registration2.Name } });
        }

        [Fact]
        public async Task Can_Get_Hierarchy()
        {
            //Arrange
            var (_, organization) = await CreateStakeHolderUserInNewOrganizationAsync();
            var (rootUuid, createdContracts) = CreateHierarchy(organization.Id);

            //Act
            var response = await ItContractV2Helper.GetHierarchyAsync(rootUuid);

            //Assert
            var hierarchy = response.ToList();
            Assert.Equal(createdContracts.Count, hierarchy.Count);

            foreach (var node in hierarchy)
            {
                var contract = Assert.Single(createdContracts, x => x.Uuid == node.Node.Uuid);
                if (contract.Uuid == rootUuid)
                {
                    Assert.Null(node.Parent);
                }
                else
                {
                    Assert.NotNull(node.Parent);
                    Assert.Equal(node.Parent.Uuid, contract.Parent.Uuid);
                }
            }
        }

        [Fact]
        public async Task Can_GET_Roles()
        {
            //Arrange
            var organization = await CreateOrganizationAsync();
            var (user, token) = await CreateApiUserAsync(organization);
            await HttpApi.SendAssignRoleToUserAsync(user.Id, OrganizationRole.LocalAdmin, organization.Id).DisposeAsync();

            var (roles, users)= await CreateRoles(organization);
            var createdContract = await ItContractV2Helper.PostContractAsync(token, new CreateNewContractRequestDTO
            {
                Name = CreateName(),
                OrganizationUuid = organization.Uuid,
                Roles = roles
            });

            //Act
            var assignedRoles = (await ItContractV2Helper.GetRoleAssignmentsInternalAsync(createdContract.Uuid)).ToList();

            //Assert
            Assert.Equal(2, assignedRoles.Count);
            Assert.Contains(assignedRoles, assignment => MatchExpectedAssignment(assignment, roles.First(), users.First()));
            Assert.Contains(assignedRoles, assignment => MatchExpectedAssignment(assignment, roles.Last(), users.Last()));
        }

        protected async Task<(string token, OrganizationDTO createdOrganization)> CreateStakeHolderUserInNewOrganizationAsync()
        {
            var organization = await CreateOrganizationAsync();

            var (_, _, token) = await HttpApi.CreateUserAndGetToken(CreateEmail(),
                OrganizationRole.User, organization.Id, true, true);
            return (token, organization);
        }

        private (Guid rootUuid, IReadOnlyList<ItContract> createdItContracts) CreateHierarchy(int orgId)
        {
            var rootContract = CreateContract(orgId);
            var childContract = CreateContract(orgId);
            var grandchildContract = CreateContract(orgId);

            var createdSystems = new List<ItContract> { rootContract, childContract, grandchildContract };

            childContract.Children = new List<ItContract>
            {
                grandchildContract
            };
            rootContract.Children = new List<ItContract>
            {
                childContract
            };

            DatabaseAccess.MutateEntitySet<ItContract>(repository =>
            {
                repository.Insert(rootContract);
            });

            return (rootContract.Uuid, createdSystems);
        }

        protected async Task<OrganizationDTO> CreateOrganizationAsync()
        {
            var organizationName = CreateName();
            var organization = await OrganizationHelper.CreateOrganizationAsync(TestEnvironment.DefaultOrganizationId, organizationName, "13370000", OrganizationTypeKeys.Kommune, AccessModifier.Public);
            return organization;
        }

        private ItContract CreateContract(int orgId)
        {
            return new ItContract
            {
                Name = CreateName(),
                OrganizationId = orgId,
                ObjectOwnerId = TestEnvironment.DefaultUserId,
                LastChangedByUserId = TestEnvironment.DefaultUserId
            };
        }

        private async Task<(User user, string token)> CreateApiUserAsync(OrganizationDTO organization)
        {
            var userAndGetToken = await HttpApi.CreateUserAndGetToken(CreateEmail(), OrganizationRole.LocalAdmin, organization.Id, true, false);
            var user = DatabaseAccess.MapFromEntitySet<User, User>(x => x.AsQueryable().ById(userAndGetToken.userId));
            return (user, userAndGetToken.token);
        }

        private async Task<(List<RoleAssignmentRequestDTO>, List<User>)> CreateRoles(OrganizationDTO organization)
        {
            var (user1, _)= await CreateApiUserAsync(organization);
            var (user2, _) = await CreateApiUserAsync(organization);
            var contractRoles = (await OptionV2ApiHelper.GetOptionsAsync(OptionV2ApiHelper.ResourceName.ItContractRoles, organization.Uuid, 10, 0)).RandomItems(2).ToList();
            var role1 = contractRoles.First();
            var role2 = contractRoles.Last();
            var roles = new List<RoleAssignmentRequestDTO>
            {
                new()
                {
                    RoleUuid = role1.Uuid,
                    UserUuid = user1.Uuid
                },
                new()
                {
                    RoleUuid = role2.Uuid,
                    UserUuid = user2.Uuid
                }
            };
            return (roles, new List<User>{user1, user2});
        }

        private static bool MatchExpectedAssignment(ExtendedRoleAssignmentResponseDTO assignment, RoleAssignmentRequestDTO expectedRole, User expectedUser)
        {
            return assignment.Role.Uuid == expectedRole.RoleUuid &&
                   assignment.User.Email == expectedUser.Email &&
                   assignment.User.Name == expectedUser.GetFullName() &&
                   assignment.User.Uuid == expectedUser.Uuid;
        }

        private string CreateEmail()
        {
            return $"{CreateName()}@kitos.dk";
        }

        private string CreateName()
        {
            return $"{nameof(ItContractsInternalApiV2Test)}{A<string>()}";
        }
    }
}
