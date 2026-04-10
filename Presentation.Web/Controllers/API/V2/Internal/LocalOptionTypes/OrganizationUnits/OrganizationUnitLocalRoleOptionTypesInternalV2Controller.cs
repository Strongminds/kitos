using System;
using System.Collections.Generic;
using System.Net;
using Core.ApplicationServices.LocalOptions;
using Core.DomainModel.LocalOptions;
using Core.DomainModel.Organization;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using Presentation.Web.Models.API.V2.Internal.Response.LocalOptions;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Web.Controllers.API.V2.Internal.LocalOptionTypes.OrganizationUnits
{
    [Route("api/v2/internal/organization-units/{organizationUuid}/local-option-types/organization-unit-roles")]
    public class OrganizationUnitLocalRoleOptionTypesInternalV2Controller: BaseLocalRoleOptionTypesInternalV2Controller<LocalOrganizationUnitRole, OrganizationUnitRight, OrganizationUnitRole>
    {
        private readonly IGenericLocalRoleOptionsService<LocalOrganizationUnitRole, OrganizationUnitRight, OrganizationUnitRole> _localOrganizationUnitRoleOptionTypeService;
        private readonly ILocalOptionTypeResponseMapper _responseMapper;
        private readonly ILocalOptionTypeWriteModelMapper _writeModelMapper;

        public OrganizationUnitLocalRoleOptionTypesInternalV2Controller(IGenericLocalRoleOptionsService<LocalOrganizationUnitRole, OrganizationUnitRight, OrganizationUnitRole> localRoleOptionTypeService, ILocalOptionTypeResponseMapper responseMapper, ILocalOptionTypeWriteModelMapper writeModelMapper) : base(localRoleOptionTypeService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetLocalOrganizationUnitRoles([NonEmptyGuid][FromRoute] Guid organizationUuid)
        {
            return GetAll(organizationUuid);

        }

        [HttpGet]
        [Route("{optionUuid}")]
        public IActionResult GetLocalOrganizationUnitRole([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromRoute] Guid optionUuid)
        {
            return GetSingle(organizationUuid, optionUuid);
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateLocalOrganizationUnitRole([NonEmptyGuid][FromRoute] Guid organizationUuid, [FromBody] LocalOptionCreateRequestDTO dto)
        {
            return Create(organizationUuid, dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchLocalOrganizationUnitRole([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid,
            [FromBody] LocalRoleOptionUpdateRequestDTO dto)
        {
            return Patch(organizationUuid, optionUuid, dto);
        }

        [HttpDelete]
        [Route("{optionUuid}")]
        public IActionResult DeleteLocalOrganizationUnitRole([NonEmptyGuid][FromRoute] Guid organizationUuid,
            [FromRoute] Guid optionUuid)
        {
            return Delete(organizationUuid, optionUuid);
        }
    }
}


