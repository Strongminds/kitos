using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Core.ApplicationServices;
using Core.ApplicationServices.ScheduledJobs;
using Core.ApplicationServices.Users.Write;
using Core.DomainModel;
using Core.DomainServices;
using Infrastructure.Services.Cryptography;
using Ninject.Activation;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V1;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.API.V1
{
    [AllowAnonymous]
    [InternalApi]
    [AllowRightsHoldersAccess]
    public class PasswordResetRequestController : BaseApiController
    {
        private readonly IUserService _userService;
        private readonly IUserWriteService _userWriteService;

        public PasswordResetRequestController(IUserService userService, IUserWriteService userWriteService)
        {
            _userService = userService;
            _userWriteService = userWriteService;
        }

        // POST api/PasswordResetRequest
        public HttpResponseMessage Post([FromBody] UserDTO input)
        {
            _userWriteService.SchedulePasswordResetRequest(input.Email);
            return NoContent();
        }

        // GET api/PasswordResetRequest
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ApiReturnDTO<PasswordResetRequestDTO>))]
        public HttpResponseMessage Get(string requestId)
        {
            try
            {
                var request = _userService.GetPasswordReset(requestId);
                if (request == null) return NotFound();
                var dto = Map<PasswordResetRequest, PasswordResetRequestDTO>(request);

                var msg = CreateResponse(HttpStatusCode.OK, dto);
                return msg;
            }
            catch (Exception e)
            {
                return CreateResponse(HttpStatusCode.InternalServerError, e);
            }
        }
    }
}
