using System;
using System.Collections.Generic;
using System.Net;
using Core.ApplicationServices.LocalOptions;
using Core.DomainModel.GDPR;
using Core.DomainModel.LocalOptions;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using Presentation.Web.Models.API.V2.Internal.Response;
using Microsoft.AspNetCore.Mvc;
using DataProcessingRegistration = Core.DomainModel.GDPR.DataProcessingRegistration;

namespace Presentation.Web.Controllers.API.V2.Internal.LocalOptionTypes.DataProcessingRegistrations
{
    [Route("api/v2/internal/data-processing/{organizationUuid}/local-option-types/country-option-types")]
    public class DprLocalCountryOptionTypesInternalV2Controller : BaseLocalRegularOptionTypesInternalV2Controller<LocalDataProcessingCountryOption, DataProcessingRegistration, DataProcessingCountryOption>
    {
        private readonly IGenericLocalOptionsService<LocalDataProcessingCountryOption, DataProcessingRegistration, DataProcessingCountryOption> _localOptionTypeService;
        private readonly ILocalOptionTypeResponseMapper _responseMapper;
        private readonly ILocalOptionTypeWriteModelMapper _writeModelMapper;

        public DprLocalCountryOptionTypesInternalV2Controller(IGenericLocalOptionsService<LocalDataProcessingCountryOption, DataProcessingRegistration, DataProcessingCountryOption> localOptionTypeService, ILocalOptionTypeResponseMapper responseMapper, ILocalOptionTypeWriteModelMapper writeModelMapper)
            : base(localOptionTypeService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetLocalCountryOptionTypes([NonEmptyGuid][FromRoute] Guid organizationUuid)
        {
            return GetAll(organizationUuid);
        }

        [HttpGet]
        [Route("{optionUuid}")]
        public IActionResult GetLocalCountryOptionTypeByOptionId([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromRoute] Guid optionUuid)
        {
            return GetSingle(organizationUuid, optionUuid);
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateLocalCountryOptionType([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromBody] LocalOptionCreateRequestDTO dto)
        {
            return Create(organizationUuid, dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchLocalCountryOptionType([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid,
            LocalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(organizationUuid, optionUuid, dto);
        }

        [HttpDelete]
        [Route("{optionUuid}")]
        public IActionResult DeleteLocalCountryOptionType([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid)
        {
            return Delete(organizationUuid, optionUuid);
        }
    }
}


