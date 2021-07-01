﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using Core.ApplicationServices.OptionTypes;
using Core.DomainModel.ItSystem;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.External.V2;
using Presentation.Web.Models.External.V2.Request;
using Presentation.Web.Models.External.V2.Response;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.External.V2.ItSystemUsages
{
    [RoutePrefix("api/v2/it-system-usage-sensitive-personal-data-types")]
    [DenyRightsHoldersAccess]
    public class ItSystemUsageSensitivePersonalDataTypeV2Controller : BaseOptionTypeV2Controller<ItSystem, SensitivePersonalDataType>
    {
        public ItSystemUsageSensitivePersonalDataTypeV2Controller(IOptionsApplicationService<ItSystem, SensitivePersonalDataType> optionService)
            : base(optionService)
        {
        }

        /// <summary>
        /// Returns It-System usage sensitive personal data types 
        /// </summary>
        /// <param name="organizationUuid">organization context for the sensitive personal data types availability</param>
        /// <returns>A list of available It-System usage sensitive personal data types</returns>
        [HttpGet]
        [Route("")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<IdentityNamePairResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult Get([RequireNonEmptyGuid] Guid organizationUuid, [FromUri] StandardPaginationQuery pagination = null)
        {
            return GetAll(organizationUuid, pagination);
        }

        /// <summary>
        /// Returns requested It-System usage sensitive personal data type
        /// </summary>
        /// <param name="sensitivePersonalDataTypeUuid">sensitive personal data type identifier</param>
        /// <param name="organizationUuid">organization context for the sensitive personal data type availability</param>
        /// <returns>A uuid and name pair with boolean to mark if the sensitive personal data type is available in the organization</returns>
        [HttpGet]
        [Route("{sensitivePersonalDataTypeUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(AvailableNamePairResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult Get([RequireNonEmptyGuid] Guid sensitivePersonalDataTypeUuid, [RequireNonEmptyGuid] Guid organizationUuid)
        {
            return GetSingle(sensitivePersonalDataTypeUuid, organizationUuid);
        }
    }
}