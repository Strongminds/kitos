using Core.ApplicationServices;
using Core.DomainModel;
using Core.DomainServices;
using Infrastructure.Services.Cryptography;
using Presentation.Web.Infrastructure.Attributes;
using Serilog.Core;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web;
using System.Web.Http;
using Core.Abstractions.Types;
using AutoMapper;
using Core.ApplicationServices.Users.Write;
using Presentation.Web.Models.API.V2.Internal.Request;
using Presentation.Web.Models.API.V2.Internal.Response;
using Core.Abstractions.Extensions;
using Presentation.Web.Controllers.API.V2.Common;

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
        public IHttpActionResult RequestPasswordReset([FromBody] PasswordResetRequestDTO request)
        {
            try
            {
                _userWriteService.RequestPasswordReset(request.Email);
                return NoContent();
            }
            catch (Exception e)
            {
                return UnknownError();
            }
        }

        [Route("{requestId}")]
        [HttpGet]
        [AllowAnonymous]
        [AllowRightsHoldersAccess]
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