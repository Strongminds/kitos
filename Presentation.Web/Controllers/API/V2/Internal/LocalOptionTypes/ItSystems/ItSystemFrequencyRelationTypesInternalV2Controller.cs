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
    [Route("api/v2/internal/it-systems/{organizationUuid}/local-option-types/frequency-relation-types")]
    public class ItSystemLocalFrequencyTypesInternalV2Controller : BaseLocalRegularOptionTypesInternalV2Controller<LocalRelationFrequencyType, SystemRelation, RelationFrequencyType>
    {
        private readonly IGenericLocalOptionsService<LocalRelationFrequencyType, SystemRelation, RelationFrequencyType> _localFrequencyRelationTypeService;
        private readonly ILocalOptionTypeResponseMapper _responseMapper;
        private readonly ILocalOptionTypeWriteModelMapper _writeModelMapper;

        public ItSystemLocalFrequencyTypesInternalV2Controller(IGenericLocalOptionsService<LocalRelationFrequencyType, SystemRelation, RelationFrequencyType> localFrequencyRelationTypeService, ILocalOptionTypeResponseMapper responseMapper, ILocalOptionTypeWriteModelMapper writeModelMapper)
            : base(localFrequencyRelationTypeService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetLocalRelationFrequencyTypes([NonEmptyGuid][FromQuery] Guid organizationUuid)
        {
            return GetAll(organizationUuid);
        }

        [HttpGet]
        [Route("{optionUuid}")]
        public IActionResult GetLocalRelationFrequencyTypeByOptionId([NonEmptyGuid][FromQuery] Guid organizationUuid, [FromQuery] Guid optionUuid)
        {
            return GetSingle(organizationUuid, optionUuid);
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateLocalRelationFrequencyType([NonEmptyGuid][FromQuery] Guid organizationUuid, [FromBody] LocalOptionCreateRequestDTO dto)
        {
            return Create(organizationUuid, dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchLocalRelationFrequencyType([NonEmptyGuid][FromQuery] Guid organizationUuid,
            [FromQuery] Guid optionUuid,
            LocalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(organizationUuid, optionUuid, dto);
        }

        [HttpDelete]
        [Route("{optionUuid}")]
        public IActionResult DeleteLocalRelationFrequencyType([NonEmptyGuid][FromQuery] Guid organizationUuid,
            [FromQuery] Guid optionUuid)
        {
            return Delete(organizationUuid, optionUuid);
        }
    }
}


