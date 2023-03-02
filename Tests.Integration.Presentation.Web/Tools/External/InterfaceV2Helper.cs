﻿using Presentation.Web.Models;
using Presentation.Web.Models.API.V2.Request;
using Presentation.Web.Models.API.V2.Response.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Presentation.Web.Models.API.V2.Request.Interface;
using Xunit;
using Presentation.Web.Models.API.V2.Response.Shared;

namespace Tests.Integration.Presentation.Web.Tools.External
{
    public class InterfaceV2Helper
    {
        private const string BasePath = "api/v2";
        private const string BasePathInterfaces = $"{BasePath}/it-interfaces";
        private const string BasePathRightHolders = $"{BasePath}/rightsholder/it-interfaces";

        public static async Task<IEnumerable<RightsHolderItInterfaceResponseDTO>> GetRightsholderInterfacesAsync(
            string token,
            int? pageSize = null,
            int? pageNumber = null,
            Guid? rightsHolder = null,
            bool? includeDeactivated = null,
            DateTime? changedSinceGtEq = null
            )
        {
            using var response = await SendGetRightsholderInterfacesAsync(token, pageSize, pageNumber, rightsHolder, includeDeactivated, changedSinceGtEq);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<IEnumerable<RightsHolderItInterfaceResponseDTO>>();
        }

        public static async Task<HttpResponseMessage> SendGetRightsholderInterfacesAsync(
            string token,
            int? pageSize = null,
            int? pageNumber = null,
            Guid? rightsHolder = null,
            bool? includeDeactivated = null,
            DateTime? changedSinceGtEq = null
            )
        {
            var path = BasePathRightHolders;
            var queryParameters = new List<KeyValuePair<string, string>>();

            if (pageSize.HasValue)
                queryParameters.Add(new KeyValuePair<string, string>("pageSize", pageSize.Value.ToString("D")));

            if (pageNumber.HasValue)
                queryParameters.Add(new KeyValuePair<string, string>("page", pageNumber.Value.ToString("D")));

            if (rightsHolder.HasValue)
                queryParameters.Add(new KeyValuePair<string, string>("rightsHolderUuid", rightsHolder.Value.ToString("D")));

            if (includeDeactivated.HasValue)
                queryParameters.Add(new KeyValuePair<string, string>("includeDeactivated", includeDeactivated.Value.ToString()));

            if (changedSinceGtEq.HasValue)
                queryParameters.Add(new KeyValuePair<string, string>("changedSinceGtEq", changedSinceGtEq.Value.ToString("O")));

            if (queryParameters.Any())
                path += $"?{string.Join("&", queryParameters.Select(x => $"{x.Key}={x.Value}"))}";

            return await HttpApi.GetWithTokenAsync(TestEnvironment.CreateUrl(path), token);
        }

        public static async Task<RightsHolderItInterfaceResponseDTO> GetRightsholderInterfaceAsync(string token, Guid interfaceGuid)
        {
            using var response = await SendGetRightsholderInterfaceAsync(token, interfaceGuid);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<RightsHolderItInterfaceResponseDTO>();
        }

        public static async Task<HttpResponseMessage> SendGetRightsholderInterfaceAsync(string token, Guid interfaceGuid)
        {
            var url = TestEnvironment.CreateUrl($"{BasePathRightHolders}/{interfaceGuid}");
            return await HttpApi.GetWithTokenAsync(url, token);
        }

        public static async Task<IEnumerable<ItInterfaceResponseDTO>> GetStakeholderInterfacesAsync(
            string token,
            int? pageSize = null,
            int? pageNumber = null,
            Guid? exposedBySystemUuid = null,
            bool? includeDeactivated = null,
            DateTime? changedSinceGtEq = null,
            string nameEquals = null,
            Guid? usedInOrganizationUuid = null
            )
        {
            using var response = await SendGetStakeholderInterfacesAsync(token, pageSize, pageNumber, exposedBySystemUuid, includeDeactivated, changedSinceGtEq, nameEquals, usedInOrganizationUuid);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<IEnumerable<ItInterfaceResponseDTO>>();
        }

        public static async Task<HttpResponseMessage> SendGetStakeholderInterfacesAsync(
            string token,
            int? pageSize = null,
            int? pageNumber = null,
            Guid? exposedBySystemUuid = null,
            bool? includeDeactivated = null,
            DateTime? changedSinceGtEq = null,
            string nameEquals = null,
            Guid? usedInOrganizationUuid = null
            )
        {
            var path = BasePathInterfaces;
            var queryParameters = new List<KeyValuePair<string, string>>();

            if (pageSize.HasValue)
                queryParameters.Add(new KeyValuePair<string, string>("pageSize", pageSize.Value.ToString("D")));

            if (pageNumber.HasValue)
                queryParameters.Add(new KeyValuePair<string, string>("page", pageNumber.Value.ToString("D")));

            if (exposedBySystemUuid.HasValue)
                queryParameters.Add(new KeyValuePair<string, string>("exposedBySystemUuid", exposedBySystemUuid.Value.ToString("D")));

            if (includeDeactivated.HasValue)
                queryParameters.Add(new KeyValuePair<string, string>("includeDeactivated", includeDeactivated.Value.ToString()));

            if (changedSinceGtEq.HasValue)
                queryParameters.Add(new KeyValuePair<string, string>("changedSinceGtEq", changedSinceGtEq.Value.ToString("O")));

            if (nameEquals != null)
                queryParameters.Add(new KeyValuePair<string, string>("nameEquals", nameEquals));

            if (usedInOrganizationUuid.HasValue)
                queryParameters.Add(new KeyValuePair<string, string>("usedInOrganizationUuid", usedInOrganizationUuid.Value.ToString("D")));

            if (queryParameters.Any())
                path += $"?{string.Join("&", queryParameters.Select(x => $"{x.Key}={x.Value}"))}";

            return await HttpApi.GetWithTokenAsync(TestEnvironment.CreateUrl(path), token);
        }

        public static async Task<ItInterfaceResponseDTO> GetStakeholderInterfaceAsync(string token, Guid interfaceGuid)
        {
            using var response = await SendGetStakeholderInterfaceAsync(token, interfaceGuid);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<ItInterfaceResponseDTO>();
        }

        public static async Task<HttpResponseMessage> SendGetStakeholderInterfaceAsync(string token, Guid interfaceGuid)
        {
            var url = TestEnvironment.CreateUrl($"{BasePathInterfaces}/{interfaceGuid}");
            return await HttpApi.GetWithTokenAsync(url, token);
        }

        public static async Task<RightsHolderItInterfaceResponseDTO> CreateRightsHolderItInterfaceAsync(string token, RightsHolderCreateItInterfaceRequestDTO request)
        {
            using var response = await SendCreateRightsHolderItInterfaceAsync(token, request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<RightsHolderItInterfaceResponseDTO>();
        }

        public static async Task<HttpResponseMessage> SendCreateRightsHolderItInterfaceAsync(string token, RightsHolderCreateItInterfaceRequestDTO request)
        {
            return await HttpApi.PostWithTokenAsync(TestEnvironment.CreateUrl(BasePathRightHolders), request, token);

        }

        public static async Task<RightsHolderItInterfaceResponseDTO> UpdateRightsHolderItInterfaceAsync(string token, Guid itInterfaceUuid, RightsHolderWritableItInterfacePropertiesDTO request)
        {
            using var response = await SendUpdateRightsHolderItInterfaceAsync(token, itInterfaceUuid, request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<RightsHolderItInterfaceResponseDTO>();
        }

        public static async Task<HttpResponseMessage> SendUpdateRightsHolderItInterfaceAsync(string token, Guid itInterfaceUuid, RightsHolderWritableItInterfacePropertiesDTO request)
        {
            return await HttpApi.PutWithTokenAsync(TestEnvironment.CreateUrl($"{BasePathRightHolders}/{itInterfaceUuid}"), token, request);
        }

        public static async Task<HttpResponseMessage> SendDeleteRightsHolderItInterfaceAsync(string token, Guid itInterfaceUuid, DeactivationReasonRequestDTO request)
        {
            return await HttpApi.DeleteWithTokenAsync(TestEnvironment.CreateUrl($"{BasePathRightHolders}/{itInterfaceUuid}"), token, request);
        }

        public static async Task<RightsHolderItInterfaceResponseDTO> PatchRightsHolderInterfaceAsync(string token, Guid uuid, params KeyValuePair<string, object>[] changedProperties)
        {
            using var response = await SendPatchUpdateRightsHolderInterfaceAsync(token, uuid, changedProperties);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<RightsHolderItInterfaceResponseDTO>();
        }

        public static async Task<HttpResponseMessage> SendPatchUpdateRightsHolderInterfaceAsync(string token, Guid uuid, params KeyValuePair<string, object>[] changedProperties)
        {
            return await HttpApi.PatchWithTokenAsync(TestEnvironment.CreateUrl($"{BasePathRightHolders}/{uuid}"), token, changedProperties.ToDictionary(x => x.Key, x => x.Value));
        }

        public static async Task<ResourcePermissionsResponseDTO> GetPermissionsAsync(string token, Guid uuid)
        {
            using var response = await HttpApi.GetWithTokenAsync(TestEnvironment.CreateUrl($"{BasePathInterfaces}/{uuid:D}/permissions"), token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<ResourcePermissionsResponseDTO>();
        }

        public static async Task<ResourceCollectionPermissionsResponseDTO> GetCollectionPermissionsAsync(string token, Guid organizationUuid)
        {
            using var response = await HttpApi.GetWithTokenAsync(TestEnvironment.CreateUrl($"{BasePathInterfaces}/permissions?organizationUuid={organizationUuid:D}"), token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.ReadResponseBodyAsAsync<ResourceCollectionPermissionsResponseDTO>();
        }
    }
}
