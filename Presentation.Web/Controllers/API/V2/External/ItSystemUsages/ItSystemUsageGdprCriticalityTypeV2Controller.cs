using System;
using System.Collections.Generic;
using System.Net;
using Core.ApplicationServices.OptionTypes;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Request.Generic.Queries;
using Presentation.Web.Models.API.V2.Response.Options;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages
{
    [Route("api/v2/it-system-usage-criticality-level-types")]
    public class ItSystemUsageSystemUsageCriticalityLevelTypeV2Controller : BaseRegularOptionTypeV2Controller<ItSystemUsage, SystemUsageCriticalityLevel>
    {
        public ItSystemUsageSystemUsageCriticalityLevelTypeV2Controller(IOptionsApplicationService<ItSystemUsage, SystemUsageCriticalityLevel> optionService)
            : base(optionService)
        {
        }

        /// <summary>
        /// Returns It-System Usage criticality level options which are available for new registrations within the organization
        /// </summary>
        /// <param name="organizationUuid">organization context for the criticality level types availability</param>
        /// <returns>A list of available It-System Usage criticality level types</returns>
        [HttpGet]
        [Route("")]
        public IActionResult Get([NonEmptyGuid] Guid organizationUuid, [FromQuery] UnboundedPaginationQuery pagination = null)
        {
            return GetAll(organizationUuid, pagination);
        }

        /// <summary>
        /// Returns requested It-System Usage criticality level type
        /// </summary>
        /// <param name="criticalityLevelTypeUuid">Criticality level type identifier</param>
        /// <param name="organizationUuid">organization context for the criticality level type availability</param>
        /// <returns>A uuid and name pair with boolean to mark if the criticality level type is available in the organization</returns>
        [HttpGet]
        [Route("{criticalityLevelTypeUuid}")]
        public IActionResult Get([NonEmptyGuid] Guid criticalityLevelTypeUuid, [NonEmptyGuid] Guid organizationUuid)
        {
            return GetSingle(criticalityLevelTypeUuid, organizationUuid);
        }
    }
}
