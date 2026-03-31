using System;
using System.Collections.Generic;
using System.Net;
using Core.ApplicationServices.LocalOptions;
using Core.DomainModel.ItSystem;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using Presentation.Web.Models.API.V2.Internal.Response;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Web.Controllers.API.V2.Internal.LocalOptionTypes.ItSystems
{
    [Route("api/v2/internal/it-systems/{organizationUuid}/local-option-types/sensitive-personal-data-types")]
    public class ItSystemLocalSensitivePersonalDataTypesInternalV2Controller : BaseLocalRegularOptionTypesInternalV2Controller<LocalSensitivePersonalDataType, ItSystem, SensitivePersonalDataType>
    {
        private readonly IGenericLocalOptionsService<LocalSensitivePersonalDataType, ItSystem, SensitivePersonalDataType> _localSensitivePersonalDataTypeService;
        private readonly ILocalOptionTypeResponseMapper _responseMapper;
        private readonly ILocalOptionTypeWriteModelMapper _writeModelMapper;

        public ItSystemLocalSensitivePersonalDataTypesInternalV2Controller(IGenericLocalOptionsService<LocalSensitivePersonalDataType, ItSystem, SensitivePersonalDataType> localSensitivePersonalDataTypeService, ILocalOptionTypeResponseMapper responseMapper, ILocalOptionTypeWriteModelMapper writeModelMapper)
            : base(localSensitivePersonalDataTypeService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetLocalSensitivePersonalDataTypes([NonEmptyGuid][FromQuery] Guid organizationUuid)
        {
            return GetAll(organizationUuid);
        }

        [HttpGet]
        [Route("{optionUuid}")]
        public IActionResult GetLocalSensitivePersonalDataTypeByOptionId([NonEmptyGuid][FromQuery] Guid organizationUuid, [FromQuery] Guid optionUuid)
        {
            return GetSingle(organizationUuid, optionUuid);
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateLocalSensitivePersonalDataType([NonEmptyGuid][FromQuery] Guid organizationUuid, LocalOptionCreateRequestDTO dto)
        {
            return Create(organizationUuid, dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchLocalSensitivePersonalDataType([NonEmptyGuid][FromQuery] Guid organizationUuid,
            [FromQuery] Guid optionUuid,
            LocalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(organizationUuid, optionUuid, dto);
        }

        [HttpDelete]
        [Route("{optionUuid}")]
        public IActionResult DeleteLocalSensitivePersonalDataType([NonEmptyGuid][FromQuery] Guid organizationUuid,
            [FromQuery] Guid optionUuid)
        {
            return Delete(organizationUuid, optionUuid);
        }
    }
}


