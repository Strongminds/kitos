﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Presentation.Web.Models.API.V2.Response.Organization;
using Presentation.Web.Models.API.V2.Types.Organization;
using Xunit;

namespace Tests.Integration.Presentation.Web.Tools.External
{
    public static class OrganizationUserV2Helper
    {
        public static async Task<IEnumerable<OrganizationUserResponseDTO>> GetOrganizationUsersAsync(string token, Guid organizationUuid, int page = 0, int pageSize = 10, string nameOrEmailQuery = null, string emailQuery = null, OrganizationUserRole? roleQuery = null)
        {
            using var response = await SendGetOrganizationUsersAsync(token, organizationUuid, page, pageSize, nameOrEmailQuery, emailQuery, roleQuery);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<IEnumerable<OrganizationUserResponseDTO>>();
        }

        public static async Task<HttpResponseMessage> SendGetOrganizationUsersAsync(string token, Guid organizationUuid, int page = 0, int pageSize = 10, string nameOrEmailQuery = null, string emailQuery = null, OrganizationUserRole? roleQuery = null)
        {
            var queryParameters = new List<KeyValuePair<string, string>>
            {
                new("page", page.ToString("D")),
                new("pageSize", pageSize.ToString("D")),
            };

            if (nameOrEmailQuery != null)
                queryParameters.Add(new KeyValuePair<string, string>("nameOrEmailQuery", nameOrEmailQuery));

            if (emailQuery != null)
                queryParameters.Add(new KeyValuePair<string, string>("emailQuery", emailQuery));

            if (roleQuery.HasValue)
                queryParameters.Add(new KeyValuePair<string, string>("roleQuery", roleQuery.Value.ToString("G")));

            var query = string.Join("&", queryParameters.Select(x => $"{x.Key}={x.Value}"));

            return await HttpApi.GetWithTokenAsync(TestEnvironment.CreateUrl($"api/v2/organizations/{organizationUuid}/users?{query}"), token);
        }


        public static async Task<OrganizationUserResponseDTO> GetOrganizationUserAsync(string token, Guid organizationUuid, Guid userUuid)
        {
            using var response = await SendGetOrganizationUserAsync(token, organizationUuid, userUuid);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<OrganizationUserResponseDTO>();
        }
        public static async Task<HttpResponseMessage> SendGetOrganizationUserAsync(string token, Guid organizationUuid, Guid userUuid)
        {
            return await HttpApi.GetWithTokenAsync(TestEnvironment.CreateUrl($"api/v2/organizations/{organizationUuid}/users/{userUuid:D}"), token);
        }
    }
}
