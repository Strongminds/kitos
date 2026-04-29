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
    [Route("api/v2/it-system-usage-gdpr-criticality-types")]
    public class ItSystemUsageGdprCriticalityTypeV2Controller : BaseRegularOptionTypeV2Controller<ItSystemUsage, GdprCriticality>
    {
        public ItSystemUsageGdprCriticalityTypeV2Controller(IOptionsApplicationService<ItSystemUsage, GdprCriticality> optionService)
            : base(optionService)
        {
        }

        /// <summary>
        /// Returns It-System Usage GDPR criticality options which are available for new registrations within the organization
        /// </summary>
        /// <param name="organizationUuid">organization context for the GDPR criticality types availability</param>
        /// <returns>A list of available It-System Usage GDPR criticality types</returns>
        [HttpGet]
        [Route("")]
        public IActionResult Get([NonEmptyGuid] Guid organizationUuid, [FromQuery] UnboundedPaginationQuery pagination = null)
        {
            return GetAll(organizationUuid, pagination);
        }

        /// <summary>
        /// Returns requested It-System Usage GDPR criticality type
        /// </summary>
        /// <param name="gdprCriticalityTypeUuid">GDPR criticality type identifier</param>
        /// <param name="organizationUuid">organization context for the GDPR criticality type availability</param>
        /// <returns>A uuid and name pair with boolean to mark if the GDPR criticality type is available in the organization</returns>
        [HttpGet]
        [Route("{gdprCriticalityTypeUuid}")]
        public IActionResult Get([NonEmptyGuid] Guid gdprCriticalityTypeUuid, [NonEmptyGuid] Guid organizationUuid)
        {
            return GetSingle(gdprCriticalityTypeUuid, organizationUuid);
        }
    }
}
