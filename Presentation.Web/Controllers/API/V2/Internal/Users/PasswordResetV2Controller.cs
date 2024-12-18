﻿using Core.ApplicationServices;
using Core.DomainModel;
using Presentation.Web.Infrastructure.Attributes;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Net;
using System.Web.Http;
using Core.Abstractions.Types;
using Core.ApplicationServices.Users.Write;
using Presentation.Web.Models.API.V2.Internal.Request;
using Presentation.Web.Models.API.V2.Internal.Response;
using Core.Abstractions.Extensions;
using Core.ApplicationServices.ScheduledJobs;

namespace Presentation.Web.Controllers.API.V2.Internal.Users
{
    [AllowAnonymous]
    [AllowRightsHoldersAccess]
    [RoutePrefix("api/v2/internal/users/password-reset")]
    public class PasswordResetInternalV2Controller : InternalApiV2Controller
    {
        private readonly IUserService _userService;
        private readonly IUserWriteService _userWriteService;

        public PasswordResetInternalV2Controller(IUserService userService, IUserWriteService userWriteService)
        {
            _userService = userService;
            _userWriteService = userWriteService;
        }

        [Route("create")]
        [HttpPost]
        [SwaggerResponse(HttpStatusCode.NoContent)]
        public IHttpActionResult RequestPasswordReset([FromBody] RequestPasswordResetRequestDTO request)
        {
            _userWriteService.SchedulePasswordResetRequest(request.Email);
            return NoContent();
        }

        [Route("{requestId}")]
        [HttpGet]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(PasswordResetResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult GetPasswordReset([FromUri] string requestId)
        {
            try
            {
                var requestResult = _userService.GetPasswordReset(requestId).FromNullable();
                if (requestResult.IsNone) return NotFound();
                var response = MapPasswordResetToResponseDTO(requestResult.Value);
                return Ok(response);
            }
            catch (Exception e)
            {
                return UnknownError();
            }
        }

        [Route("{requestId}")]
        [HttpPost]
        [SwaggerResponse(HttpStatusCode.NoContent)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        public IHttpActionResult PostPasswordReset([FromUri] string requestId, [FromBody] ResetPasswordRequestDTO request)
        {
            try
            {
                var resetRequest = _userService.GetPasswordReset(requestId);
                _userService.ResetPassword(resetRequest, request.Password);
                return NoContent();
            }
            catch (Exception e)
            {
                return UnknownError();
            }
        }

        private IHttpActionResult UnknownError()
        {
            return FromOperationFailure(OperationFailure.UnknownError);
        }

        private PasswordResetResponseDTO MapPasswordResetToResponseDTO(PasswordResetRequest request)
        {
            return new PasswordResetResponseDTO
            {
                Email = request.User.Email
            };
        }
    }
}