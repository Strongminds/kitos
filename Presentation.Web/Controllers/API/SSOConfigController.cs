﻿using System.Net.Http;
using System.Web.Http;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.API
{
    [PublicApi]
    public class SSOConfigController : BaseApiController
    {
        [AllowAnonymous]
        public HttpResponseMessage Get()
        {
            var SSOGateway = System.Web.Configuration.WebConfigurationManager.AppSettings["SSOGateway"];
            var SSOAudience = System.Web.Configuration.WebConfigurationManager.AppSettings["SSOAudience"];

            var ssoConfig = new { SSOGateway = SSOGateway, SSOAudience = SSOAudience };
            return Ok(ssoConfig);
        }
    }
}