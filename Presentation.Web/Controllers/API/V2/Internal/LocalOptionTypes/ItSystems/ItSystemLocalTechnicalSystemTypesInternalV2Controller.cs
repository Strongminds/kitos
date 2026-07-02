using System.Collections.Generic;
using System;
using Core.ApplicationServices.LocalOptions;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.LocalOptions;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Response;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Web.Controllers.API.V2.Internal.LocalOptionTypes.ItSystems
{
    [Route("api/v2/internal/it-systems/{organizationUuid}/local-option-types/technical-system-types")]
    public class ItSystemLocalTechnicalSystemTypesInternalV2Controller : BaseLocalRegularOptionTypesInternalV2Controller<LocalTechnicalSystemType, ItSystemUsage, TechnicalSystemType>
    {
        public ItSystemLocalTechnicalSystemTypesInternalV2Controller(
            IGenericLocalOptionsService<LocalTechnicalSystemType, ItSystemUsage, TechnicalSystemType> localTechnicalSystemTypeService,
            ILocalOptionTypeResponseMapper responseMapper,
            ILocalOptionTypeWriteModelMapper writeModelMapper)
            : base(localTechnicalSystemTypeService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        [ProducesResponseType(typeof(IEnumerable<LocalRegularOptionResponseDTO>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetLocalTechnicalSystemTypes([NonEmptyGuid][FromRoute] Guid organizationUuid)
        {
            return GetAll(organizationUuid);
        }

        [HttpGet]
        [Route("{optionUuid}")]
        [ProducesResponseType(typeof(LocalRegularOptionResponseDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult GetLocalTechnicalSystemTypeByOptionId([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromRoute] Guid optionUuid)
        {
            return GetSingle(organizationUuid, optionUuid);
        }

        [HttpPost]
        [Route("")]
        [ProducesResponseType(typeof(LocalRegularOptionResponseDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult CreateTechnicalSystemType([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromBody] LocalOptionCreateRequestDTO dto)
        {
            return Create(organizationUuid, dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        [ProducesResponseType(typeof(LocalRegularOptionResponseDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult PatchTechnicalSystemType([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid,
            [FromBody] LocalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(organizationUuid, optionUuid, dto);
        }

        [HttpDelete]
        [Route("{optionUuid}")]
        [ProducesResponseType(typeof(LocalRegularOptionResponseDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public IActionResult DeleteTechnicalSystemType([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid)
        {
            return Delete(organizationUuid, optionUuid);
        }
    }
}
