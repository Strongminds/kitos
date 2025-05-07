﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel.ItSystemUsage.GDPR;
using Core.DomainModel.ItSystemUsage.Read;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V1;
using Presentation.Web.Models.API.V1.ItSystemUsage;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools
{
    public class ItSystemUsageHelper
    {
        public static async Task<IEnumerable<ItSystemUsageOverviewReadModel>> QueryReadModelByNameContent(int organizationId, string nameContent, int top, int skip, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl($"odata/Organizations({organizationId})/ItSystemUsageOverviewReadModels?$expand=RoleAssignments,ItSystemTaskRefs,SensitiveDataLevels,ArchivePeriods,DataProcessingRegistrations,DependsOnInterfaces,IncomingRelatedItSystemUsages,OutgoingRelatedItSystemUsages,RelevantOrganizationUnits,AssociatedContracts&$filter=contains(SystemName,'{nameContent}')&$top={top}&$skip={skip}&$orderBy=SystemName"), cookie);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadOdataListResponseBodyAsAsync<ItSystemUsageOverviewReadModel>();
        }

        public static async Task<IEnumerable<ItSystemUsageOverviewReadModel>> QueryReadModelByNameContent(Guid organizationUuid, string nameContent, int top, int skip, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl($"odata/ItSystemUsageOverviewReadModels?organizationUuid={organizationUuid:D}&$expand=RoleAssignments,ItSystemTaskRefs,SensitiveDataLevels,ArchivePeriods,DataProcessingRegistrations,DependsOnInterfaces,IncomingRelatedItSystemUsages,OutgoingRelatedItSystemUsages,RelevantOrganizationUnits,AssociatedContracts&$filter=contains(SystemName,'{nameContent}')&$top={top}&$skip={skip}&$orderBy=SystemName"), cookie);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadOdataListResponseBodyAsAsync<ItSystemUsageOverviewReadModel>();
        }

        public static async Task<ItSystemUsageDTO> GetItSystemUsageRequestAsync(int systemUsageId)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            using (var okResponse = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl($"api/itsystemusage/{systemUsageId}"), cookie))
            {
                Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
                return await okResponse.ReadResponseBodyAsKitosApiResponseAsync<ItSystemUsageDTO>();
            }
        }

        public static async Task<ItSystemUsageSensitiveDataLevelDTO> AddSensitiveDataLevel(int systemUsageId, SensitiveDataLevel sensitiveDataLevel)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            using var okResponse = await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/v1/itsystemusage/{systemUsageId}/sensitivityLevel/add"), cookie, sensitiveDataLevel);
            Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
            return await okResponse.ReadResponseBodyAsKitosApiResponseAsync<ItSystemUsageSensitiveDataLevelDTO>();
        }

        public static async Task<ItSystemUsageDTO> PatchSystemUsage(int usageSystemId, int orgId, object body)
        {
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            using var okResponse = await HttpApi.PatchWithCookieAsync(TestEnvironment.CreateUrl($"api/itsystemusage/{usageSystemId}?organizationId={orgId}"), cookie, body);
            Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
            return await okResponse.ReadResponseBodyAsKitosApiResponseAsync<ItSystemUsageDTO>();
        }

        public static async Task<IEnumerable<BusinessRoleDTO>> GetAvailableRolesAsync(int orgId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl($"odata/LocalItSystemRoles?$filter=IsLocallyAvailable eq true or IsObligatory eq true&$orderby=Priority desc&organizationId={orgId}"), cookie);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadOdataListResponseBodyAsAsync<BusinessRoleDTO>();
        }

        public static async Task<IEnumerable<UserDTO>> GetAvailableUsersAsync(int orgId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.GetWithCookieAsync(TestEnvironment.CreateUrl($"odata/Organizations({orgId})/Organizations.GetUsers?$select=Id,Name,LastName,Email"), cookie);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadOdataListResponseBodyAsAsync<UserDTO>();
        }

        public static async Task<HttpResponseMessage> SendAssignRoleRequestAsync(int systemUsageId, int orgId, int roleId, int userId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            return await HttpApi.PostWithCookieAsync(
                TestEnvironment.CreateUrl($"api/itSystemUsageRights/{systemUsageId}?organizationId={orgId}"), cookie,
                new 
                {
                    roleId = roleId,
                    userId = userId
                });
        }

        public static async Task<HttpResponseMessage> SendOdataDeleteRightRequestAsync(int rightId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            return await HttpApi.DeleteWithCookieAsync(TestEnvironment.CreateUrl($"odata/ItSystemRights({rightId})"), cookie);
        }

        public static async Task<HttpResponseMessage> SendSetResponsibleOrganizationUnitRequestAsync(int systemUsageId, int orgUnitId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.PostWithCookieAsync(
                    TestEnvironment.CreateUrl($"api/itSystemUsageOrgUnitUsage/?usageId={systemUsageId}&orgUnitId={orgUnitId}&responsible=true"), 
                    cookie,
                    new { } // No body for this call
                    );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return response;
        }

        public static async Task<HttpResponseMessage> SendAddOrganizationUnitRequestAsync(int systemUsageId, int orgUnitId, int orgId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.PostWithCookieAsync(
                    TestEnvironment.CreateUrl($"api/itSystemUsage/{systemUsageId}?organizationunit={orgUnitId}&organizationId={orgId}"),
                    cookie,
                    new { } // No body for this call
                    );
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return response;
        }

        public static async Task<HttpResponseMessage> SendSetMainContractRequestAsync(int systemUsageId, int contractId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.PostWithCookieAsync(
                    TestEnvironment.CreateUrl($"api/ItContractItSystemUsage/?contractId={contractId}&usageId={systemUsageId}"),
                    cookie,
                    new { } // No body for this call
                    );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return response;
        }
    }
}