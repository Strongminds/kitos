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
    [Route("api/v2/internal/data-processing/{organizationUuid}/local-option-types/data-responsible-types")]
    public class DprLocalDataResponsibleTypesInternalV2Controller : BaseLocalRegularOptionTypesInternalV2Controller<LocalDataProcessingDataResponsibleOption, DataProcessingRegistration, DataProcessingDataResponsibleOption>
    {
        private readonly IGenericLocalOptionsService<LocalDataProcessingDataResponsibleOption, DataProcessingRegistration, DataProcessingDataResponsibleOption> _localOptionTypeService;
        private readonly ILocalOptionTypeResponseMapper _responseMapper;
        private readonly ILocalOptionTypeWriteModelMapper _writeModelMapper;

        public DprLocalDataResponsibleTypesInternalV2Controller(IGenericLocalOptionsService<LocalDataProcessingDataResponsibleOption, DataProcessingRegistration, DataProcessingDataResponsibleOption> localOptionTypeService, ILocalOptionTypeResponseMapper responseMapper, ILocalOptionTypeWriteModelMapper writeModelMapper)
            : base(localOptionTypeService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetLocalDataResponsibleTypes([NonEmptyGuid][FromRoute] Guid organizationUuid)
        {
            return GetAll(organizationUuid);
        }

        [HttpGet]
        [Route("{optionUuid}")]
        public IActionResult GetLocalDataResponsibleTypeByOptionId([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromRoute] Guid optionUuid)
        {
            return GetSingle(organizationUuid, optionUuid);
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateLocalDataResponsibleType([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromBody] LocalOptionCreateRequestDTO dto)
        {
            return Create(organizationUuid, dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchLocalDataResponsibleType([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid,
            [FromBody] LocalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(organizationUuid, optionUuid, dto);
        }

        [HttpDelete]
        [Route("{optionUuid}")]
        public IActionResult DeleteLocalDataResponsibleType([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid)
        {
            return Delete(organizationUuid, optionUuid);
        }
    }
}


