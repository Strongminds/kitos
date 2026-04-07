using System.Collections.Generic;
using System.Net;
using System;
using Core.ApplicationServices.GlobalOptions;
using Core.DomainModel.Organization;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Controllers.API.V2.Internal.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using Presentation.Web.Models.API.V2.Internal.Response.GlobalOptions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Web.Controllers.API.V2.Internal.GlobalOptionTypes
{
    [Route("api/v2/internal/organizations/global-option-types/country-codes")]

    public class OrganizationGlobalCountryCodesInternalV2Controller
        : BaseGlobalRegularOptionTypesInternalV2Controller<Organization, CountryCode>
    {
        public OrganizationGlobalCountryCodesInternalV2Controller(IGlobalRegularOptionsService<CountryCode, Organization> globalRegularOptionsService, IGlobalOptionTypeResponseMapper responseMapper, IGlobalOptionTypeWriteModelMapper writeModelMapper) 
            : base(globalRegularOptionsService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetCountryCodes()
        {
            return GetAll();
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateCountryCode([FromBody] GlobalRegularOptionCreateRequestDTO dto)
        {
            return Create(dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchCountryCode([NonEmptyGuid][FromQuery] Guid optionUuid,
            GlobalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(optionUuid, dto);
        }
    }
}


