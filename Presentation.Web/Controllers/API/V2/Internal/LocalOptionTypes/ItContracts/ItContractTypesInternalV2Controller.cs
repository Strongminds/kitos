using System;
using System.Collections.Generic;
using System.Net;
using Core.ApplicationServices.LocalOptions;
using Core.DomainModel.ItContract;
using Core.DomainModel.LocalOptions;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using Presentation.Web.Models.API.V2.Internal.Response;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Web.Controllers.API.V2.Internal.LocalOptionTypes.ItContracts
{
    [Route("api/v2/internal/it-contracts/{organizationUuid}/local-option-types/it-contract-types")]
    public class ItContractLocalContractTypesInternalV2Controller : BaseLocalRegularOptionTypesInternalV2Controller<LocalItContractType, ItContract, ItContractType>
    {
        private readonly IGenericLocalOptionsService<LocalItContractType, ItContract, ItContractType> _localOptionTypeService;
        private readonly ILocalOptionTypeResponseMapper _responseMapper;
        private readonly ILocalOptionTypeWriteModelMapper _writeModelMapper;

        public ItContractLocalContractTypesInternalV2Controller(IGenericLocalOptionsService<LocalItContractType, ItContract, ItContractType> localOptionTypeService, ILocalOptionTypeResponseMapper responseMapper, ILocalOptionTypeWriteModelMapper writeModelMapper)
            : base(localOptionTypeService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetLocalContractTypes([NonEmptyGuid][FromQuery] Guid organizationUuid)
        {
            return GetAll(organizationUuid);
        }

        [HttpGet]
        [Route("{optionUuid}")]
        public IActionResult GetLocalContractTypeByOptionId([NonEmptyGuid][FromQuery] Guid organizationUuid, [FromQuery] Guid optionUuid)
        {
            return GetSingle(organizationUuid, optionUuid);
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateLocalContractType([NonEmptyGuid][FromQuery] Guid organizationUuid, LocalOptionCreateRequestDTO dto)
        {
            return Create(organizationUuid, dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchLocalContractType([NonEmptyGuid][FromQuery] Guid organizationUuid,
            [FromQuery] Guid optionUuid,
            LocalRegularOptionUpdateRequestDTO dto)
        {
            return Patch(organizationUuid, optionUuid, dto);
        }

        [HttpDelete]
        [Route("{optionUuid}")]
        public IActionResult DeleteLocalContractType([NonEmptyGuid][FromQuery] Guid organizationUuid,
            [FromQuery] Guid optionUuid)
        {
            return Delete(organizationUuid, optionUuid);
        }
    }
}


