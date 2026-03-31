using Presentation.Web.Infrastructure.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Presentation.Web.Properties;

namespace Presentation.Web.Controllers.API.V1
{
    [AllowAnonymous]
    [InternalApi]
    public class HealthCheckController : ControllerBase
    {
        private static readonly string DeploymentVersion = Settings.Default.DeploymentVersion;

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(DeploymentVersion);
        }
    }
}



