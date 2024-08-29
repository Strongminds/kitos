﻿using Core.ApplicationServices.Organizations;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.Swagger.Annotations;
using System.Net;
using System;
using System.Linq;
using System.Web.Http;
using Core.ApplicationServices.Organizations.Write;
using Presentation.Web.Models.API.V2.Internal.Response.OrganizationUnit;
using Presentation.Web.Models.API.V2.Response.Organization;
using Presentation.Web.Controllers.API.V2.Internal.OrganizationUnits.Mapping;
using Presentation.Web.Models.API.V2.Request.OrganizationUnit;
using System.Web.Http.Results;

namespace Presentation.Web.Controllers.API.V2.Internal.OrganizationUnits
{
    /// <summary>
    /// Internal API for the organization units in KITOS
    /// </summary>
    [RoutePrefix("api/v2/internal/organizations/{organizationUuid}/organization-units")]
    public class OrganizationUnitsInternalV2Controller : InternalApiV2Controller
    {
        private readonly IOrganizationUnitWriteService _organizationUnitWriteService;
        private readonly IOrganizationUnitService _organizationUnitService;
        private readonly IOrganizationUnitWriteModelMapper _organizationUnitWriteModelMapper;
        private readonly IOrganizationUnitResponseModelMapper _responseMapper;

        public OrganizationUnitsInternalV2Controller(IOrganizationUnitWriteService organizationUnitWriteService,
            IOrganizationUnitWriteModelMapper organizationUnitWriteModelMapper,
            IOrganizationUnitService organizationUnitService, IOrganizationUnitResponseModelMapper responseMapper)
        {
            _organizationUnitWriteService = organizationUnitWriteService;
            _organizationUnitWriteModelMapper = organizationUnitWriteModelMapper;
            _organizationUnitService = organizationUnitService;
            _responseMapper = responseMapper;
        }

        [Route("{unitUuid}/permissions")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(UnitAccessRightsResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetPermissions([NonEmptyGuid] Guid organizationUuid, [NonEmptyGuid] Guid unitUuid)
        {
            return _organizationUnitService.GetAccessRights(organizationUuid, unitUuid)
                .Select(accessRights => new UnitAccessRightsResponseDTO(accessRights))
                .Match(Ok, FromOperationError);
        }

        [Route("all/collection-permissions")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(UnitAccessRightsResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetCollectionPermissions([NonEmptyGuid] Guid organizationUuid)
        {
            return _organizationUnitService.GetAccessRightsByOrganization(organizationUuid)
                .Select(accessRightsWithUnits => accessRightsWithUnits.Select(accessRightWithUnit => 
                    new UnitAccessRightsWithUnitDataResponseDTO
                    (
                        accessRightWithUnit.UnitAccessRights,
                        _responseMapper.ToUnitDto(accessRightWithUnit.OrganizationUnit)
                    )
                ))
                .Match(Ok, FromOperationError);
        }

        [Route("create")]
        [SwaggerResponse(HttpStatusCode.Created, Type = typeof(OrganizationUnitResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult CreateUnit([NonEmptyGuid] Guid organizationUuid, [FromBody] CreateOrganizationUnitRequestDTO parameters)
        {
            return _organizationUnitWriteService.Create(organizationUuid, _organizationUnitWriteModelMapper.FromPOST(parameters))
                .Select(_responseMapper.ToUnitDto)
                .Match(MapUnitCreatedResponse, FromOperationError);
        }

        [Route("hierarchy")]
        [SwaggerResponse((HttpStatusCode.OK))]
        [SwaggerResponse((HttpStatusCode.NotFound))]
        [SwaggerResponse((HttpStatusCode.BadRequest))]
        [SwaggerResponse((HttpStatusCode.Unauthorized))]
        public IHttpActionResult GetHierarchy([NonEmptyGuid] Guid organizationUuid)
        {
            _organizationUnitService.GetHierarchy(organizationUuid);
            return Ok();
        }

        private CreatedNegotiatedContentResult<OrganizationUnitResponseDTO> MapUnitCreatedResponse(OrganizationUnitResponseDTO dto)
        {
            return Created($"{Request.RequestUri.AbsoluteUri.TrimEnd('/')}/{dto.Uuid}", dto);
        }
    }
}