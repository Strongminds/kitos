using Core.ApplicationServices.GlobalOptions;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Controllers.API.V2.Internal.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using System.Collections.Generic;
using System.Net;
using System;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Models.API.V2.Internal.Response.GlobalOptions;

namespace Presentation.Web.Controllers.API.V2.Internal.GlobalOptionTypes.ItSystems
{
    [Route("api/v2/internal/it-systems/global-option-types/it-system-categories")]

    public class ItSystemGlobalItSystemCategoriesInternalV2Controller: BaseGlobalRegularOptionTypesInternalV2Controller<ItSystemUsage, ItSystemCategories>
    {
        public ItSystemGlobalItSystemCategoriesInternalV2Controller(IGlobalRegularOptionsService<ItSystemCategories, ItSystemUsage> globalRegularOptionsService, IGlobalOptionTypeResponseMapper responseMapper, IGlobalOptionTypeWriteModelMapper writeModelMapper) : base(globalRegularOptionsService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetGlobalItSystemCategoriess()
        {
            return GetAll();
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateGlobalItSystemCategories([FromBody] GlobalRegularOptionCreateRequestDTO dto)
        {
            return Create(dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchGlobalItSystemCategories([NonEmptyGuid][FromRoute] Guid optionUuid,
            GlobalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(optionUuid, dto);
        }
    }
}
    


