using System;
using System.Collections.Generic;
using System.Net;
using Core.ApplicationServices.LocalOptions;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.LocalOptions;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using Presentation.Web.Models.API.V2.Internal.Response;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Web.Controllers.API.V2.Internal.LocalOptionTypes.ItSystems
{
    [Route("api/v2/internal/it-systems/{organizationUuid}/local-option-types/archive-location-types")]
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
        [Route("")]
        public IActionResult GetLocalArchiveLocationTypes([NonEmptyGuid][FromRoute] Guid organizationUuid)
        {
            return GetAll(organizationUuid);
        }

        [HttpGet]
        [Route("{optionUuid}")]
        public IActionResult GetLocalArchiveLocationByOptionId([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromRoute] Guid optionUuid)
        {
            return GetSingle(organizationUuid, optionUuid);
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateArchiveLocationType([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromBody] LocalOptionCreateRequestDTO dto)
        {
            return Create(organizationUuid, dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchArchiveLocationType([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid,
            [FromBody] LocalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(organizationUuid, optionUuid, dto);
        }

        [HttpDelete]
        [Route("{optionUuid}")]
        public IActionResult DeleteArchiveLocationType([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid)
        {
            return Delete(organizationUuid, optionUuid);
        }
    }
}


