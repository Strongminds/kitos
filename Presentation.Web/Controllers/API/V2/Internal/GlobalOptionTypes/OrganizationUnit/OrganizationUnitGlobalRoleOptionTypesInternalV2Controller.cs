using Core.ApplicationServices.GlobalOptions;
using Core.DomainModel.Organization;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Controllers.API.V2.Internal.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request;
using Presentation.Web.Models.API.V2.Internal.Request.Options;
using Presentation.Web.Models.API.V2.Internal.Response.GlobalOptions;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Presentation.Web.Controllers.API.V2.Internal.GlobalOptionTypes.OrganizationUnit
{
    [Route("api/v2/internal/organization/global-option-types/organization-unit-roles")]
    public class OrganizationUnitGlobalRoleOptionTypesInternalV2Controller : BaseGlobalRoleOptionTypesInternalV2Controller<OrganizationUnitRole, OrganizationUnitRight>
    {
        public OrganizationUnitGlobalRoleOptionTypesInternalV2Controller(IGlobalRoleOptionsService<OrganizationUnitRole, OrganizationUnitRight> globalRoleOptionsService, IGlobalOptionTypeResponseMapper responseMapper, IGlobalOptionTypeWriteModelMapper writeModelMapper) : base(globalRoleOptionsService, responseMapper, writeModelMapper)
        {
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetOrganizationUnitRoles()
        {
            return GetAll();
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateOrganizationUnitRole([FromBody] GlobalRoleOptionCreateRequestDTO dto)
        {
            return Create(dto);
        }

        [HttpPatch]
        [Route("{optionUuid}")]
        public IActionResult PatchGlobalOrganizationUnitRole([NonEmptyGuid][FromRoute] Guid optionUuid,
            GlobalRoleOptionUpdateRequestDTO dto)
        {
            return Patch(optionUuid, dto);
        }
    }
}


