using System;
using System.Collections.Generic;
using System.Net;
using Core.ApplicationServices.LocalOptions;
using Core.DomainModel.GDPR;
using Core.DomainModel.LocalOptions;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using Presentation.Web.Models.API.V2.Internal.Response.LocalOptions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Web.Controllers.API.V2.Internal.LocalOptionTypes.DataProcessingRegistrations
{
    [Route("api/v2/internal/data-processing/{organizationUuid}/local-option-types/dpr-roles")]
    public class DprLocalRoleOptionTypesInternalV2Controller : BaseLocalRoleOptionTypesInternalV2Controller<LocalDataProcessingRegistrationRole, DataProcessingRegistrationRight, DataProcessingRegistrationRole>
    {
        private readonly IGenericLocalRoleOptionsService<LocalDataProcessingRegistrationRole, DataProcessingRegistrationRight, DataProcessingRegistrationRole> _localDprRoleOptionTypeService;
        private readonly ILocalOptionTypeResponseMapper _responseMapper;
        private readonly ILocalOptionTypeWriteModelMapper _writeModelMapper;

        public DprLocalRoleOptionTypesInternalV2Controller(IGenericLocalRoleOptionsService<LocalDataProcessingRegistrationRole, DataProcessingRegistrationRight, DataProcessingRegistrationRole> localRoleOptionTypeService, ILocalOptionTypeResponseMapper responseMapper, ILocalOptionTypeWriteModelMapper writeModelMapper) : base(localRoleOptionTypeService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetLocalDprRoles([NonEmptyGuid][FromRoute] Guid organizationUuid)
        {
            return GetAll(organizationUuid);
        }

        [HttpGet]
        [Route("{optionUuid}")]
        public IActionResult GetLocalDprRole([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromRoute] Guid optionUuid)
        {
            return GetSingle(organizationUuid, optionUuid);
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateLocalDprRole([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromBody] LocalOptionCreateRequestDTO dto)
        {
            return Create(organizationUuid, dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchLocalDprRole([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid,
            LocalRoleOptionUpdateRequestDTO dto)
        {
            return Patch(organizationUuid, optionUuid, dto);
        }

        [HttpDelete]
        [Route("{optionUuid}")]
        public IActionResult DeleteLocalDprRole([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid)
        {
            return Delete(organizationUuid, optionUuid);
        }
    }
}


