﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V1;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools
{
    public class OrganizationUnitHelper
    {
        public static async Task<OrgUnitDTO> GetOrganizationUnitsAsync(int orgId, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var orgUnitUrl = TestEnvironment.CreateUrl($"api/OrganizationUnit?organization={orgId}");

            using var response = await HttpApi.GetWithCookieAsync(orgUnitUrl, cookie);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsKitosApiResponseAsync<OrgUnitDTO>();
        }

        public static async Task<List<OrganizationUnitRole>> GetOrganizationUnitRolesAsync(Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var orgUnitUrl = TestEnvironment.CreateUrl("odata/OrganizationUnitRoles");

            using var response = await HttpApi.GetWithCookieAsync(orgUnitUrl, cookie);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadOdataListResponseBodyAsAsync<OrganizationUnitRole>();
        }

        public static async Task DeleteUnitAsync(Guid organizationUuid, Guid unitUuid, Cookie optionalLogin = null)
        {
            var cookie = optionalLogin ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var orgUnitUrl = TestEnvironment.CreateUrl($"api/v1/organizations/{organizationUuid}/organization-units/{unitUuid}");

            using var response = await HttpApi.DeleteWithCookieAsync(orgUnitUrl, cookie);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
