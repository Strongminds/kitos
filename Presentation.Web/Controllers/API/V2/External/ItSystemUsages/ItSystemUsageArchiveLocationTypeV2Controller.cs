﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using Core.ApplicationServices.OptionTypes;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Request.Generic.Queries;
using Presentation.Web.Models.API.V2.Response.Options;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages
{
    [RoutePrefix("api/v2/it-system-usage-archive-location-types")]
    public class ItSystemUsageArchiveLocationTypeV2Controller : BaseRegularOptionTypeV2Controller<ItSystemUsage, ArchiveLocation>
    {
        public ItSystemUsageArchiveLocationTypeV2Controller(IOptionsApplicationService<ItSystemUsage, ArchiveLocation> optionService)
            : base(optionService)
        {
        }

        /// <summary>
        /// Returns It-System Usage archive location options which are available for new registrations within the organization
        /// </summary>
        /// <param name="organizationUuid">organization context for the archive locations availability</param>
        /// <returns>A list of available It-System Usage archive locations</returns>
        [HttpGet]
        [Route]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<RegularOptionResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult Get([NonEmptyGuid] Guid organizationUuid, [FromUri] UnboundedPaginationQuery pagination = null)
        {
            return GetAll(organizationUuid, pagination);
        }

        /// <summary>
        /// Returns requested It-System Usage archive location
        /// </summary>
        /// <param name="archiveLocationUuid">archive location identifier</param>
        /// <param name="organizationUuid">organization context for the archive location availability</param>
        /// <returns>A uuid and name pair with boolean to mark if the archive location is available in the organization</returns>
        [HttpGet]
        [Route("{archiveLocationUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(RegularOptionExtendedResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult Get([NonEmptyGuid] Guid archiveLocationUuid, [NonEmptyGuid] Guid organizationUuid)
        {
            return GetSingle(archiveLocationUuid, organizationUuid);
        }
    }
}