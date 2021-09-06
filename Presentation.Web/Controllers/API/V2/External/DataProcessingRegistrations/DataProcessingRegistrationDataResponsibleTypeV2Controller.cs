﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using Core.ApplicationServices.OptionTypes;
using Core.DomainModel.GDPR;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Request.Generic.Queries;
using Presentation.Web.Models.API.V2.Response.Generic.Identity;
using Presentation.Web.Models.API.V2.Response.Options;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.API.V2.External.DataProcessingRegistrations
{
    [RoutePrefix("api/v2/data-processing-registration-data-responsible-types")]
    public class DataProcessingRegistrationDataResponsibleTypeV2Controller : BaseRegularOptionTypeV2Controller<DataProcessingRegistration, DataProcessingDataResponsibleOption>
    {
        public DataProcessingRegistrationDataResponsibleTypeV2Controller(IOptionsApplicationService<DataProcessingRegistration, DataProcessingDataResponsibleOption> optionService)
            : base(optionService)
        {
        }

        /// <summary>
        /// Returns Data Processing Registration data responsible options which are available for new registrations within the organization
        /// </summary>
        /// <param name="organizationUuid">organization context for the data responsible availability</param>
        /// <returns>A list of available Data Processing Registration data responsible</returns>
        [HttpGet]
        [Route]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<IdentityNamePairResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult Get([NonEmptyGuid] Guid organizationUuid, [FromUri] UnboundedPaginationQuery pagination = null)
        {
            return GetAll(organizationUuid, pagination);
        }

        /// <summary>
        /// Returns requested Data Processing Registration data responsible
        /// </summary>
        /// <param name="dataResponsibleUuid">data responsible identifier</param>
        /// <param name="organizationUuid">organization context for the data responsible availability</param>
        /// <returns>A uuid and name pair with boolean to mark if the data responsible is available in the organization</returns>
        [HttpGet]
        [Route("{dataResponsibleUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(RegularOptionExtendedResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult Get([NonEmptyGuid] Guid dataResponsibleUuid, [NonEmptyGuid] Guid organizationUuid)
        {
            return GetSingle(dataResponsibleUuid, organizationUuid);
        }
    }
}