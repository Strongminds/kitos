﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V2.Request.System.Regular;

namespace Tests.Integration.Presentation.Web.Tools.External
{
    public class DBSV2Helper
    {
        private const string ControllerRoutePrefix = "api/v2/it-systems";

        public static async Task<HttpResponseMessage> PatchSystem(Guid systemUuid,
            DbsUpdateRequestDTO request, string token = null)
        {
            var requestToken = token ?? (await HttpApi.GetTokenAsync(OrganizationRole.GlobalAdmin)).Token;
            var cookie = await HttpApi.GetCookieAsync(OrganizationRole.GlobalAdmin);
            var url = GetUrl(systemUuid);
            return await HttpApi.PatchWithTokenAsync(url, requestToken, request);
        }

        private static Uri GetUrl(Guid systemUuid)
        {
            return TestEnvironment.CreateUrl($"{ControllerRoutePrefix}/{systemUuid}/dbs");
        }
    }
}
