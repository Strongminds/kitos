﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using Core.ApplicationServices.LocalOptions;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.LocalOptions;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using Presentation.Web.Models.API.V2.Internal.Response;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.API.V2.Internal.LocalOptionTypes.ItSystems
{
    [RoutePrefix("api/v2/internal/it-systems/{organizationUuid}/local-option-types/archive-location-types")]
    public class ItSystemLocalArchiveLocationTypesInternalV2Controller : BaseLocalRegularOptionTypesInternalV2Controller<LocalArchiveLocation, ItSystemUsage, ArchiveLocation>
    {
        private readonly IGenericLocalOptionsService<LocalArchiveLocation, ItSystemUsage, ArchiveLocation> _localArchiveLocationTypeService;
        private readonly ILocalOptionTypeResponseMapper _responseMapper;
        private readonly ILocalOptionTypeWriteModelMapper _writeModelMapper;

        public ItSystemLocalArchiveLocationTypesInternalV2Controller(IGenericLocalOptionsService<LocalArchiveLocation, ItSystemUsage, ArchiveLocation> localArchiveLocationTypeService, ILocalOptionTypeResponseMapper responseMapper, ILocalOptionTypeWriteModelMapper writeModelMapper)
            : base(localArchiveLocationTypeService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<LocalRegularOptionResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult GetLocalArchiveLocationTypes([NonEmptyGuid][FromUri] Guid organizationUuid)
        {
            return GetAll(organizationUuid);
        }

        [HttpGet]
        [Route("{optionUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(LocalRegularOptionResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult GetLocalArchiveLocationByOptionId([NonEmptyGuid][FromUri] Guid organizationUuid, [FromUri] Guid optionUuid)
        {
            return GetSingle(organizationUuid, optionUuid);
        }

        [HttpPost]
        [Route]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(LocalRegularOptionResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult CreateArchiveLocationType([NonEmptyGuid][FromUri] Guid organizationUuid, LocalOptionCreateRequestDTO dto)
        {
            return Create(organizationUuid, dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(LocalRegularOptionResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult PatchArchiveLocationType([NonEmptyGuid][FromUri] Guid organizationUuid,
            [FromUri] Guid optionUuid,
            LocalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(organizationUuid, optionUuid, dto);
        }

        [HttpDelete]
        [Route("{optionUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(LocalRegularOptionResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult DeleteArchiveLocationType([NonEmptyGuid][FromUri] Guid organizationUuid,
            [FromUri] Guid optionUuid)
        {
            return Delete(organizationUuid, optionUuid);
        }
    }
}