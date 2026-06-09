using Core.ApplicationServices.GlobalOptions;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Controllers.API.V2.Internal.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using System;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Web.Controllers.API.V2.Internal.GlobalOptionTypes.ItSystems
{
    [Route("api/v2/internal/it-systems/global-option-types/technical-system-types")]
    public class ItSystemGlobalTechnicalSystemTypesInternalV2Controller : BaseGlobalRegularOptionTypesInternalV2Controller<
        ItSystemUsage,
        TechnicalSystemType>
    {
        public ItSystemGlobalTechnicalSystemTypesInternalV2Controller(
            IGlobalRegularOptionsService<TechnicalSystemType, ItSystemUsage> globalRegularOptionsService,
            IGlobalOptionTypeResponseMapper responseMapper, IGlobalOptionTypeWriteModelMapper writeModelMapper) : base(
            globalRegularOptionsService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetTechnicalSystemTypes()
        {
            return GetAll();
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateTechnicalSystemType([FromBody] GlobalRegularOptionCreateRequestDTO dto)
        {
            return Create(dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchTechnicalSystemType([NonEmptyGuid] [FromRoute] Guid optionUuid,
            [FromBody] GlobalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(optionUuid, dto);
        }
    }
}
