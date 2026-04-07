using Core.ApplicationServices.GlobalOptions;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Controllers.API.V2.Internal.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using System.Collections.Generic;
using System.Net;
using System;
using Core.DomainModel.ItSystem;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Models.API.V2.Internal.Response.GlobalOptions;

namespace Presentation.Web.Controllers.API.V2.Internal.GlobalOptionTypes.ItSystems
{
    [Route("api/v2/internal/it-systems/global-option-types/data-types")]

    public class ItSystemGlobalDataTypesInternalV2Controller: BaseGlobalRegularOptionTypesInternalV2Controller<DataRow, DataType>
    {
        public ItSystemGlobalDataTypesInternalV2Controller(IGlobalRegularOptionsService<DataType, DataRow> globalRegularOptionsService, IGlobalOptionTypeResponseMapper responseMapper, IGlobalOptionTypeWriteModelMapper writeModelMapper) : base(globalRegularOptionsService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetGlobalDataTypes()
        {
            return GetAll();
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateGlobalDataType([FromBody] GlobalRegularOptionCreateRequestDTO dto)
        {
            return Create(dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchGlobalDataType([NonEmptyGuid][FromQuery] Guid optionUuid,
            GlobalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(optionUuid, dto);
        }
    }
}
    


