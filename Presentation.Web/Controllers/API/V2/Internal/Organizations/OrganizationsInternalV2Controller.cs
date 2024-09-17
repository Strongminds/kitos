﻿using System;
using System.Net;
using Core.ApplicationServices.Organizations;
using System.Web.Http;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.Swagger.Annotations;
using Presentation.Web.Controllers.API.V2.External.Generic;
using Presentation.Web.Models.API.V2.Response.Shared;
using Presentation.Web.Models.API.V2.Internal.Response.Roles;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Core.ApplicationServices.Model.Organizations.Write;
using Core.ApplicationServices.Model.Shared;
using Core.DomainModel.Organization;
using Presentation.Web.Controllers.API.V2.Internal.Mapping;
using Presentation.Web.Models.API.V2.Request.Organization;
using Presentation.Web.Models.API.V2.Response.Organization;
using OrganizationType = Presentation.Web.Models.API.V2.Types.Organization.OrganizationType;

namespace Presentation.Web.Controllers.API.V2.Internal.Organizations
{
    /// <summary>
    /// Internal API for the organizations in KITOS
    /// </summary>
    [RoutePrefix("api/v2/internal/organizations")]
    public class OrganizationsInternalV2Controller : InternalApiV2Controller
    {
        private readonly IOrganizationService _organizationService;
        private readonly IResourcePermissionsResponseMapper _permissionsResponseMapper;
        private readonly IOrganizationMapper _organizationMapper;

        public OrganizationsInternalV2Controller(IOrganizationService organizationService, IResourcePermissionsResponseMapper permissionsResponseMapper, IOrganizationMapper organizationMapper)
        {
            _organizationService = organizationService;
            _permissionsResponseMapper = permissionsResponseMapper;
            _organizationMapper = organizationMapper;
        }

        [Route("{organizationUuid}permissions")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ResourcePermissionsResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetPermissions([NonEmptyGuid] Guid organizationUuid)
        {
            return _organizationService.GetPermissions(organizationUuid)
                .Select(_permissionsResponseMapper.Map)
                .Match(Ok, FromOperationError);
        }

        [HttpPatch]
        [Route("{organizationUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(OrganizationResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult UpdateOrganizationMasterData([FromUri] [NonEmptyGuid] Guid organizationUuid, OrganizationMasterDataRequestDTO requestDto)
        {
            if (!ModelState.IsValid) return BadRequest();
            
            var updateParameters = ToUpdateParameters(requestDto);
            return _organizationService.UpdateOrganization(organizationUuid, updateParameters)
                .Select(_organizationMapper.ToDTO)
                .Match(Ok, FromOperationError);
        }

        private OrganizationUpdateParameters ToUpdateParameters(OrganizationMasterDataRequestDTO dto)
        {
            return new()
            {
                Cvr = OptionalValueChange<string>.With(dto.Cvr),
                Email = OptionalValueChange<string>.With(dto.Email),
                Address = OptionalValueChange<string>.With(dto.Address),
                Phone = OptionalValueChange<string>.With(dto.Phone),
            };
        }
    }
}