﻿using Core.DomainModel.Organization;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Presentation.Web.Models.API.V2.Internal.Request.User;
using Presentation.Web.Models.API.V2.Request.User;
using Xunit;
using Presentation.Web.Models.API.V2.Internal.Response.User;
using System.Linq;
using Presentation.Web.Models.API.V2.Response.Organization;

namespace Tests.Integration.Presentation.Web.Tools.Internal.Users
{
    public static class UsersV2Helper
    {
        public static async Task<UserResponseDTO> CreateUser(Guid organizationUuid, CreateUserRequestDTO request, Cookie cookie = null)
        {
            var requestCookie = cookie ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.PostWithCookieAsync(
                TestEnvironment.CreateUrl(
                    $"{ControllerPrefix(organizationUuid)}/create"), requestCookie, request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<UserResponseDTO>();
        }

        public static async Task<UserResponseDTO> UpdateUser(Guid organizationUuid, Guid userUuid,
            object request, Cookie cookie = null)
        {
            var requestCookie = cookie ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.PatchWithCookieAsync(
                TestEnvironment.CreateUrl(
                    $"{ControllerPrefix(organizationUuid)}/{userUuid}/patch"), requestCookie, request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<UserResponseDTO>();
        }

        public static async Task<HttpResponseMessage> SendNotification(Guid organizationUuid, Guid userUuid,
            Cookie cookie = null)
        {
            var requestCookie = cookie ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl($"{ControllerPrefix(organizationUuid)}/{userUuid}/notifications/send");
            return await HttpApi.PostWithCookieAsync(url, requestCookie, null);
        }

        public static async Task<HttpStatusCode> DeleteUserAndVerifyStatusCode(Guid organizationUuid, Guid userUuid, Cookie cookie = null, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var requestCookie = cookie ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl(
                $"{ControllerPrefix(organizationUuid)}/{userUuid}");
            using var response = await HttpApi.DeleteWithCookieAsync(url,
                requestCookie);

            var statusCode = response.StatusCode;
            Assert.Equal(expectedStatusCode, statusCode);
            return statusCode;
        }

        public static async Task DeleteUserGlobally(Guid userUuid, Cookie cookie = null)
        {
            var requestCookie = cookie ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl(
                $"api/v2/internal/users/{userUuid}");
            using var response = await HttpApi.DeleteWithCookieAsync(url,
                requestCookie);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        public static async Task<UserCollectionPermissionsResponseDTO> GetUserCollectionPermissions(Guid organizationUuid, Cookie cookie = null)
        {
            var requestCookie = cookie ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            using var response = await HttpApi.GetWithCookieAsync(
                TestEnvironment.CreateUrl(
                    $"{ControllerPrefix(organizationUuid)}/permissions"), requestCookie);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<UserCollectionPermissionsResponseDTO>();
        }

        public static async Task<UserIsPartOfCurrentOrgResponseDTO> GetUserByEmail(Guid organizationUuid, string email, Cookie cookie = null)
        {
            using var response = await SendGetUserByEmail(organizationUuid, email, cookie);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<UserIsPartOfCurrentOrgResponseDTO>();
        }

        public static async Task<HttpResponseMessage> SendGetUserByEmail(Guid organizationUuid, string email, Cookie cookie = null)
        {
            var requestCookie = cookie ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            return await HttpApi.GetWithCookieAsync(
                TestEnvironment.CreateUrl(
                    $"{ControllerPrefix(organizationUuid)}/find-any-by-email?email={email}"), requestCookie);
        }

        public static async Task<IEnumerable<UserReferenceResponseDTO>> GetUsers(string email)
        {
            var requestCookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);

            var queryParameters = new List<KeyValuePair<string, string>>
            {
                new ("emailQuery", email)
            };
            
            var query = string.Join("&", queryParameters.Select(x => $"{x.Key}={x.Value}"));

            using var response = await HttpApi.GetWithCookieAsync(
                TestEnvironment.CreateUrl(
                    $"{GlobalUserControllerPrefix()}/search?{query}"), requestCookie);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<IEnumerable<UserReferenceResponseDTO>>();
        }

        public static async Task<IEnumerable<OrganizationResponseDTO>> GetUserOrganization(Guid userUuid)
        {
            var requestCookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            
            using var response = await HttpApi.GetWithCookieAsync(
                TestEnvironment.CreateUrl(
                    $"{GlobalUserControllerPrefix()}/{userUuid}/organizations"), requestCookie);

            var resp = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<IEnumerable<OrganizationResponseDTO>>();
        }

        public static async Task<HttpResponseMessage> CopyRoles(Guid organizationUuid, Guid fromUser, Guid toUser,
            MutateUserRightsRequestDTO request)
        {
            var requestCookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl($"{ControllerPrefix(organizationUuid)}/{fromUser}/copy-roles/{toUser}");
            return await HttpApi.PostWithCookieAsync(url, requestCookie, request);
        }

        public static async Task<HttpResponseMessage> TransferRoles(Guid organizationUuid, Guid fromUser, Guid toUser,
            MutateUserRightsRequestDTO request)
        {
            var requestCookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl($"{ControllerPrefix(organizationUuid)}/{fromUser}/transfer-roles/{toUser}");
            return await HttpApi.PostWithCookieAsync(url, requestCookie, request);
        }

        public static async Task<IEnumerable<UserReferenceResponseDTO>> GetGlobalAdmins()
        {
            var requestCookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl($"{GlobalUserControllerPrefix()}/global-admins");
            using var response = await HttpApi.GetWithCookieAsync(url, requestCookie);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.ReadResponseBodyAsAsync<IEnumerable<UserReferenceResponseDTO>>();
        }

        public static async Task<HttpResponseMessage> AddGlobalAdmin(Guid userUuid, OrganizationRole role = OrganizationRole.GlobalAdmin)
        {
            var cookie = await HttpApi.GetCookieAsync(role);
            var url = TestEnvironment.CreateUrl($"{GlobalUserControllerPrefix()}/global-admins/{userUuid}");
            return await HttpApi.PostWithCookieAsync(url, cookie, null);
        }

        public static async Task<HttpResponseMessage> RemoveGlobalAdmin(Guid userUuid, Cookie cookie = null)
        {
            var requestCookie = cookie ?? await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = TestEnvironment.CreateUrl($"{GlobalUserControllerPrefix()}/global-admins/{userUuid}");
            return await HttpApi.DeleteWithCookieAsync(url, requestCookie, null);
        }

        private static string ControllerPrefix(Guid organizationUuid)
        {
            return $"api/v2/internal/organization/{organizationUuid}/users";
        }

        private static string GlobalUserControllerPrefix()
        {
            return $"api/v2/internal/users";
        }


    }
}
