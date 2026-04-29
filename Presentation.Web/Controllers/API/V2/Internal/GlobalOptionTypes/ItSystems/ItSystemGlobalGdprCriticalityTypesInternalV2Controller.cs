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
    [Route("api/v2/internal/it-systems/global-option-types/gdpr-criticality-types")]
    public class ItSystemGlobalGdprCriticalityTypesInternalV2Controller : BaseGlobalRegularOptionTypesInternalV2Controller<
        ItSystemUsage,
        GdprCriticality>
    {
        public ItSystemGlobalGdprCriticalityTypesInternalV2Controller(
            IGlobalRegularOptionsService<GdprCriticality, ItSystemUsage> globalRegularOptionsService,
            IGlobalOptionTypeResponseMapper responseMapper, IGlobalOptionTypeWriteModelMapper writeModelMapper) : base(
            globalRegularOptionsService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetGdprCriticalityTypes()
        {
            return GetAll();
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateGdprCriticalityType([FromBody] GlobalRegularOptionCreateRequestDTO dto)
        {
            return Create(dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchGdprCriticalityType([NonEmptyGuid] [FromRoute] Guid optionUuid,
            [FromBody] GlobalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(optionUuid, dto);
        }
    }
}
