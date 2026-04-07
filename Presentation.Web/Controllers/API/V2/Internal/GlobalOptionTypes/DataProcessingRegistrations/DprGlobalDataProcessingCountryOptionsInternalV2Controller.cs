using Core.ApplicationServices.GlobalOptions;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Controllers.API.V2.Internal.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using System.Collections.Generic;
using System.Net;
using System;
using Core.DomainModel.GDPR;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Models.API.V2.Internal.Response.GlobalOptions;

namespace Presentation.Web.Controllers.API.V2.Internal.GlobalOptionTypes.DataProcessingRegistrations
{
    [Route("api/v2/internal/dpr/global-option-types/data-processing-country-options")]

    public class DprGlobalDataProcessingCountryOptionsInternalV2Controller: BaseGlobalRegularOptionTypesInternalV2Controller<DataProcessingRegistration, DataProcessingCountryOption>
    {
        public DprGlobalDataProcessingCountryOptionsInternalV2Controller(IGlobalRegularOptionsService<DataProcessingCountryOption, DataProcessingRegistration> globalRegularOptionsService, IGlobalOptionTypeResponseMapper responseMapper, IGlobalOptionTypeWriteModelMapper writeModelMapper) : base(globalRegularOptionsService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetGlobalDataProcessingCountryOptions()
        {
            return GetAll();
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateGlobalDataProcessingCountryOption([FromBody] GlobalRegularOptionCreateRequestDTO dto)
        {
            return Create(dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchGlobalDataProcessingCountryOption([NonEmptyGuid][FromQuery] Guid optionUuid,
            GlobalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(optionUuid, dto);
        }
    }
}
    


