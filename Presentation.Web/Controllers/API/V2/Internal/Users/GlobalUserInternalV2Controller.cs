using Core.ApplicationServices.Users.Write;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.Swagger.Annotations;
using System.Net;
using System;
using System.Web.Http;
using Core.Abstractions.Types;

namespace Presentation.Web.Controllers.API.V2.Internal.Users
{
    /// <summary>
    /// Internal API for managing users in all of KITOS
    /// </summary>
    [RoutePrefix("api/v2/internal/users")]
    public class GlobalUserInternalV2Controller : InternalApiV2Controller
    {
        private readonly IUserWriteService _userWriteService;

        public GlobalUserInternalV2Controller(IUserWriteService userWriteService)
        {
            _userWriteService = userWriteService;
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
    }
}