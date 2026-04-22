using System;
using System.Collections.Generic;
using System.Net;
using Core.ApplicationServices.LocalOptions;
using Core.DomainModel.ItContract;
using Core.DomainModel.LocalOptions;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using Presentation.Web.Models.API.V2.Internal.Response.LocalOptions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Web.Controllers.API.V2.Internal.LocalOptionTypes.ItContracts
{
    [Route("api/v2/internal/it-contracts/{organizationUuid}/local-option-types/it-contract-roles")]
    public class ItContractLocalRoleOptionTypesInternalV2Controller : BaseLocalRoleOptionTypesInternalV2Controller<LocalItContractRole, ItContractRight, ItContractRole>
    {
        private readonly IGenericLocalRoleOptionsService<LocalItContractRole, ItContractRight, ItContractRole> _localItContractRoleOptionTypeService;
        private readonly ILocalOptionTypeResponseMapper _responseMapper;
        private readonly ILocalOptionTypeWriteModelMapper _writeModelMapper;

        public ItContractLocalRoleOptionTypesInternalV2Controller(IGenericLocalRoleOptionsService<LocalItContractRole, ItContractRight, ItContractRole> localRoleOptionTypeService, ILocalOptionTypeResponseMapper responseMapper, ILocalOptionTypeWriteModelMapper writeModelMapper) : base(localRoleOptionTypeService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetLocalItContractRoles([NonEmptyGuid][FromRoute] Guid organizationUuid)
        {
            return GetAll(organizationUuid);
        }

        [HttpGet]
        [Route("{optionUuid}")]
        public IActionResult GetLocalItContractRole([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromRoute] Guid optionUuid)
        {
            return GetSingle(organizationUuid, optionUuid);
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateLocalItContractRole([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromBody] LocalOptionCreateRequestDTO dto)
        {
            return Create(organizationUuid, dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchLocalItContractRole([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid,
            [FromBody] LocalRoleOptionUpdateRequestDTO dto)
        {
            return Patch(organizationUuid, optionUuid, dto);
        }

        [HttpDelete]
        [Route("{optionUuid}")]
        public IActionResult DeleteLocalItContractRole([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid)
        {
            return Delete(organizationUuid, optionUuid);
        }
    }
}


