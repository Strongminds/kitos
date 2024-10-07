﻿using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.Swagger.Annotations;
using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Core.ApplicationServices.Users.Write;
using Presentation.Web.Controllers.API.V2.Internal.Users.Mapping;
using Presentation.Web.Models.API.V2.Internal.Response.User;
using Presentation.Web.Models.API.V2.Request.User;
using System.Web.Http.Results;
using Core.ApplicationServices;
using Core.ApplicationServices.Model.Users;
using Core.DomainModel.Organization;
using Presentation.Web.Models.API.V2.Internal.Request.User;

namespace Presentation.Web.Controllers.API.V2.Internal.Users
{
    /// <summary>
    /// Internal API for the users in KITOS
    /// </summary>
    [RoutePrefix("api/v2/internal/organization/{organizationUuid}/users")]
    public class UsersInternalV2Controller : InternalApiV2Controller
    {
        private readonly IUserWriteModelMapper _writeModelMapper;
        private readonly IUserWriteService _userWriteService;
        private readonly IUserResponseModelMapper _userResponseModelMapper;
        private readonly IUserService _userService;

        public UsersInternalV2Controller(IUserWriteModelMapper writeModelMapper, 
            IUserWriteService userWriteService, 
            IUserResponseModelMapper userResponseModelMapper, 
            IUserService userService)
        {
            _writeModelMapper = writeModelMapper;
            _userWriteService = userWriteService;
            _userResponseModelMapper = userResponseModelMapper;
            _userService = userService;
        }

        [Route("create")]
        [HttpPost]
        [SwaggerResponse(HttpStatusCode.Created, Type = typeof(UserResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult CreateUser([NonEmptyGuid] Guid organizationUuid, [FromBody] CreateUserRequestDTO parameters)
        {
            return _userWriteService.Create(organizationUuid, _writeModelMapper.FromPOST(parameters))
                .Select(user => _userResponseModelMapper.ToUserResponseDTO(organizationUuid, user))
                .Match(MapUserCreatedResponse, FromOperationError);
        }

        [Route("{userUuid}/patch")]
        [HttpPatch]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(UserResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult PatchUser([NonEmptyGuid] Guid organizationUuid, [NonEmptyGuid] Guid userUuid,
            [FromBody] UpdateUserRequestDTO parameters)
        {
            return _userWriteService.Update(organizationUuid, userUuid, _writeModelMapper.FromPATCH(parameters))
                .Select(user => _userResponseModelMapper.ToUserResponseDTO(organizationUuid, user))
                .Match(Ok, FromOperationError);
        }

        [Route("{userUuid}/notifications/send")]
        [HttpPost]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult SendNotification([NonEmptyGuid] Guid organizationUuid, [NonEmptyGuid] Guid userUuid)
        {
            return _userWriteService.SendNotification(organizationUuid, userUuid)
                .Match(FromOperationError, Ok);
        }

        [Route("permissions")]
        [HttpGet]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(UserCollectionPermissionsResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetCollectionPermissions([NonEmptyGuid] Guid organizationUuid)
        {
            return _userWriteService.GetCollectionPermissions(organizationUuid)
                .Select(MapUserCollectionPermissionsResponseDto)
                .Match(Ok, FromOperationError);
        }

        [Route("find-any-by-email")]
        [HttpGet]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(UserIsPartOfCurrentOrgResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetUsersByEmailInOtherOrganizations([NonEmptyGuid] Guid organizationUuid, string email)
        {
            return _userService.GetUserByEmail(organizationUuid, email)
                .Select(user => _userResponseModelMapper.ToUserWithIsPartOfCurrentOrgResponseDTO(organizationUuid, user))
                .Match(Ok, FromOperationError);
        }

        [Route("{fromUserUuid}/copy-roles/{toUserUuid}")]
        [HttpPost]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult CopyRoles([NonEmptyGuid] Guid organizationUuid, [NonEmptyGuid] Guid fromUserUuid, [NonEmptyGuid] Guid toUserUuid, [FromBody] CopyUserRightsRequestDTO request)
        {
            var parameters = MapCopyRightsDTOToParameters(request);
            return _userWriteService.CopyUserRights(organizationUuid, fromUserUuid, toUserUuid, parameters)
                .Match(FromOperationError, Ok);
        }

        [Route("{userUuid}")]
        [HttpDelete]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult DeleteUser([NonEmptyGuid] Guid organizationUuid, [NonEmptyGuid] Guid userUuid)
        {
            return _userService.DeleteUserByOrganizationUuid(userUuid, organizationUuid)
                .Match(FromOperationError,
                    Ok);
        }

        private CreatedNegotiatedContentResult<UserResponseDTO> MapUserCreatedResponse(UserResponseDTO dto)
        {
            return Created($"{Request.RequestUri.AbsoluteUri.TrimEnd('/')}/{dto.Uuid}", dto);
        }

        private UserCollectionPermissionsResponseDTO MapUserCollectionPermissionsResponseDto(
            UserCollectionPermissionsResult permissions)
        {
            return new UserCollectionPermissionsResponseDTO(permissions.Create, permissions.Edit, permissions.Delete);

        }

        private UserRightsChangeParameters MapCopyRightsDTOToParameters(CopyUserRightsRequestDTO request)
        {
            var unitRights = MapUserRightsDTOToRoleIdSet(request.UnitRights);
            var systemRights = MapUserRightsDTOToRoleIdSet(request.SystemRights);
            var contractRights = MapUserRightsDTOToRoleIdSet(request.ContractRights);
            var dprRights = MapUserRightsDTOToRoleIdSet(request.DataProcessingRights);
            return new UserRightsChangeParameters(new List<OrganizationRole>(), dprRights, systemRights, contractRights, unitRights);
        }

        private IEnumerable<int> MapUserRightsDTOToRoleIdSet(IEnumerable<CopyRightRequestDTO> rights)
        {
            return rights.Select(right => right.RoleId);
        }
    }
}
