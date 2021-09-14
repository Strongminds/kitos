﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using Core.ApplicationServices.OptionTypes;
using Core.DomainModel.ItSystem;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Request.Generic.Queries;
using Presentation.Web.Models.API.V2.Response.Options;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages
{
    [RoutePrefix("api/v2/it-system-usage-role-types")]
    public class ItSystemUsageRoleTypeV2Controller : BaseRoleOptionTypeV2Controller<ItSystemRight, ItSystemRole>
    {
        public ItSystemUsageRoleTypeV2Controller(IOptionsApplicationService<ItSystemRight, ItSystemRole> optionApplicationService)
            : base(optionApplicationService)
        {
            
        }

        /// <summary>
        /// Returns IT-System usage role types which are available for new registrations within the organization
        /// </summary>
        /// <param name="organizationUuid">organization context for the IT-System usage role availability</param>
        /// <returns>A list of available IT-System usage role types</returns>
        [HttpGet]
        [Route("")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<RoleOptionResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult Get([NonEmptyGuid] Guid organizationUuid, [FromUri] UnboundedPaginationQuery pagination = null)
        {
            return GetAll(organizationUuid, pagination);
        }

        /// <summary>
        /// Returns requested IT-System usage role type
        /// </summary>
        /// <param name="systemUsageRoleTypeUuid">role type identifier</param>
        /// <param name="organizationUuid">organization context for the role type availability</param>
        /// <returns>A detailed description of the role type and it's availability</returns>
        [HttpGet]
        [Route("{systemUsageRoleTypeUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(RoleOptionExtendedResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult Get([NonEmptyGuid] Guid systemUsageRoleTypeUuid, [NonEmptyGuid] Guid organizationUuid)
        {
            return GetSingle(systemUsageRoleTypeUuid, organizationUuid);
        }
    }
}