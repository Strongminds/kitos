using System;
using System.Collections.Generic;
using System.Net;
using Core.ApplicationServices.LocalOptions;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.LocalOptions;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using Presentation.Web.Models.API.V2.Internal.Response;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Web.Controllers.API.V2.Internal.LocalOptionTypes.ItSystems
{
    [Route("api/v2/internal/it-systems/{organizationUuid}/local-option-types/local-register-types")]
    public class ItSystemLocalRegisterTypesInternalV2Controller : BaseLocalRegularOptionTypesInternalV2Controller<LocalRegisterType, ItSystemUsage, RegisterType>
    {
        private readonly IGenericLocalOptionsService<LocalRegisterType, ItSystemUsage, RegisterType> _localRegisterTypeService;
        private readonly ILocalOptionTypeResponseMapper _responseMapper;
        private readonly ILocalOptionTypeWriteModelMapper _writeModelMapper;

        public ItSystemLocalRegisterTypesInternalV2Controller(IGenericLocalOptionsService<LocalRegisterType, ItSystemUsage, RegisterType> localRegisterTypeService, ILocalOptionTypeResponseMapper responseMapper, ILocalOptionTypeWriteModelMapper writeModelMapper)
            : base(localRegisterTypeService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetLocalRegisterTypes([NonEmptyGuid][FromRoute] Guid organizationUuid)
        {
            return GetAll(organizationUuid);
        }

        [HttpGet]
        [Route("{optionUuid}")]
        public IActionResult GetLocalRegisterTypeByOptionId([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromRoute] Guid optionUuid)
        {
            return GetSingle(organizationUuid, optionUuid);
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateLocalRegisterType([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromBody] LocalOptionCreateRequestDTO dto)
        {
            return Create(organizationUuid, dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchLocalRegisterType([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid,
            LocalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(organizationUuid, optionUuid, dto);
        }

        [HttpDelete]
        [Route("{optionUuid}")]
        public IActionResult DeleteLocalRegisterType([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid)
        {
            return Delete(organizationUuid, optionUuid);
        }
    }
}


