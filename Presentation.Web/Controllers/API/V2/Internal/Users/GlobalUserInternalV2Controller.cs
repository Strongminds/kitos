using Core.ApplicationServices.Users.Write;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.Swagger.Annotations;
using System.Net;
using System;
using System.Web.Http;
using Core.Abstractions.Types;
using Core.DomainModel;
using Core.DomainServices.Queries.User;
using Core.DomainServices.Queries;
using Presentation.Web.Models.API.V2.Request.Generic.Queries;
using Presentation.Web.Models.API.V2.Types.Shared;
using System.Collections.Generic;
using System.Linq;
using Presentation.Web.Models.API.V2.Internal.Response.User;
using Core.ApplicationServices;
using Presentation.Web.Extensions;
using Presentation.Web.Controllers.API.V2.Internal.Mapping;
using System.Web.Http.Results;

namespace Presentation.Web.Controllers.API.V2.Internal.Users
{
    /// <summary>
    /// Internal API for managing users in all of KITOS
    /// </summary>
    [RoutePrefix("api/v2/internal/users")]
    public class GlobalUserInternalV2Controller : InternalApiV2Controller
    {
        private readonly IUserWriteService _userWriteService;
        private readonly IUserService _userService;

        public GlobalUserInternalV2Controller(IUserWriteService userWriteService, 
            IUserService userService)
        {
            _userWriteService = userWriteService;
            _userService = userService;
        }

        [Route("{userUuid}")]
        [HttpDelete]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        public IHttpActionResult DeleteUserInOrganization([NonEmptyGuid] Guid userUuid)
        {
            return _userWriteService.DeleteUser(userUuid, Maybe<Guid>.None)
                .Match(FromOperationError, Ok);
        }

        [Route("")]
        [HttpGet]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<UserReferenceResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetUsers(
            string nameOrEmailQuery = null,
            string emailQuery = null,
            CommonOrderByProperty? orderByProperty = null,
            [FromUri] BoundedPaginationQuery paginationQuery = null)
        {
            var queries = new List<IDomainQuery<User>>();

            if (!string.IsNullOrWhiteSpace(nameOrEmailQuery))
                queries.Add(new QueryUserByNameOrEmail(nameOrEmailQuery));

            if (!string.IsNullOrWhiteSpace(emailQuery))
                queries.Add(new QueryUserByEmail(emailQuery));

            var result = _userService
                .GetUsers(queries.ToArray());
            result = result.OrderUserApiResults(orderByProperty);
            result = result.Page(paginationQuery);
            return Ok(result.ToList().Select(InternalDtoModelV2MappingExtensions.MapUserReferenceResponseDTO));
        }

        [Route("global-admins")]
        [HttpGet]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<UserReferenceResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetGlobalAdmins()
        {
            return _userService.GetUsersWithRoleAssignedInAnyOrganization(Core.DomainModel.Organization.OrganizationRole.GlobalAdmin)
                .Select(users => users.Select(InternalDtoModelV2MappingExtensions.MapUserReferenceResponseDTO))
                .Match(Ok, FromOperationError);
        }

        [Route("global-admins/{userUuid}")]
        [HttpPost]
        [SwaggerResponse(HttpStatusCode.NoContent)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult AddGlobalAdmin([FromUri][NonEmptyGuid] Guid userUuid)
        {
            return _userWriteService.AddGlobalAdmin(userUuid)
                        .Match(NoContent, FromOperationError);
        }

        [Route("global-admins/{userUuid}")]
        [HttpDelete]
        [SwaggerResponse(HttpStatusCode.NoContent)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult RemoveGlobalAdmin([FromUri][NonEmptyGuid] Guid userUuid)
        {
            return _userWriteService.RemoveGlobalAdmin(userUuid)
                        .Match(FromOperationError, NoContent);
        }
    }
}