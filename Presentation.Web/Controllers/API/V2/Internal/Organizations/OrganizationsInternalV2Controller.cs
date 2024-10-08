﻿using System;
using System.Net;
using Core.ApplicationServices.Organizations;
using System.Web.Http;
using Core.ApplicationServices.Organizations.Write;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.Swagger.Annotations;
using Presentation.Web.Models.API.V2.Response.Shared;
using Presentation.Web.Models.API.V2.Internal.Request.Organizations;
using Presentation.Web.Models.API.V2.Response.Organization;
using Presentation.Web.Models.API.V2.Internal.Response.Organizations;
using Presentation.Web.Controllers.API.V2.Common.Mapping;

namespace Presentation.Web.Controllers.API.V2.Internal.Organizations
{
    /// <summary>
    /// Internal API for the organizations in KITOS
    /// </summary>
    [RoutePrefix("api/v2/internal/organizations/{organizationUuid}")]
    public class OrganizationsInternalV2Controller : InternalApiV2Controller
    {
        private readonly IOrganizationService _organizationService;
        private readonly IOrganizationWriteService _organizationWriteService;
        private readonly IOrganizationResponseMapper _organizationResponseMapper;
        private readonly IOrganizationWriteModelMapper _organizationWriteModelMapper;

        public OrganizationsInternalV2Controller(IOrganizationService organizationService, IOrganizationResponseMapper organizationResponseMapper, IOrganizationWriteModelMapper organizationWriteModelMapper, IOrganizationWriteService organizationWriteService)
        {
            _organizationService = organizationService;
            _organizationResponseMapper = organizationResponseMapper;
            _organizationWriteModelMapper = organizationWriteModelMapper;
            _organizationWriteService = organizationWriteService;
        }

        [Route("permissions")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(OrganizationPermissionsResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetPermissions([NonEmptyGuid] Guid organizationUuid)
        {
            return _organizationService.GetPermissions(organizationUuid)
                .Select(_organizationResponseMapper.ToPermissionsDTO)
                .Match(Ok, FromOperationError);
        }

        [HttpPatch]
        [Route("masterData")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(OrganizationMasterDataResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult UpdateOrganizationMasterData([FromUri] [NonEmptyGuid] Guid organizationUuid, OrganizationMasterDataRequestDTO requestDto)
        {
            if (!ModelState.IsValid) return BadRequest();
            
            var updateParameters = _organizationWriteModelMapper.ToMasterDataUpdateParameters(requestDto);
            return _organizationWriteService.UpdateMasterData(organizationUuid, updateParameters)
                .Select(_organizationResponseMapper.ToMasterDataDTO)
                .Match(Ok, FromOperationError);
        }

        [Route("masterData")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(OrganizationMasterDataResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetOrganizationMasterData([FromUri] [NonEmptyGuid] Guid organizationUuid)
        {
            if (!ModelState.IsValid) return BadRequest();

            return _organizationService.GetOrganization(organizationUuid)
                .Select(_organizationResponseMapper.ToMasterDataDTO)
                .Match(Ok, FromOperationError);
        }

        [Route("masterData/roles")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(OrganizationMasterDataRolesResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetOrganizationMasterDataRoles([FromUri][NonEmptyGuid] Guid organizationUuid)
        {
            if (!ModelState.IsValid) return BadRequest();

            return _organizationWriteService.GetOrCreateOrganizationMasterDataRoles(organizationUuid)
                .Select(_organizationResponseMapper.ToRolesDTO)
                .Match(Ok, FromOperationError);
        }
        
        [HttpPatch]
        [Route("masterData/roles")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(OrganizationMasterDataRolesResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult UpsertOrganizationMasterDataRoles([FromUri][NonEmptyGuid] Guid organizationUuid, OrganizationMasterDataRolesRequestDTO requestDto)
        {
            if (!ModelState.IsValid) return BadRequest();

            var updateParameters = _organizationWriteModelMapper.ToMasterDataRolesUpdateParameters(organizationUuid, requestDto);
            return _organizationWriteService.UpsertOrganizationMasterDataRoles(organizationUuid, updateParameters)
                .Select(_organizationResponseMapper.ToRolesDTO)
                .Match(Ok, FromOperationError);
        }
    }
}
