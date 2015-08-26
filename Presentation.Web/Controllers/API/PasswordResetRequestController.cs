﻿using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Core.DomainModel;
using Core.DomainServices;
using Presentation.Web.Models;

namespace Presentation.Web.Controllers.API
{
    [AllowAnonymous]
    public class PasswordResetRequestController : BaseApiController
    {
        private readonly IUserService _userService;
        private readonly IUserRepository _userRepository;

        public PasswordResetRequestController(IUserService userService, IUserRepository userRepository)
        {
            _userService = userService;
            _userRepository = userRepository;
        }

        // POST api/PasswordResetRequest
        public HttpResponseMessage Post([FromBody] UserDTO input)
        {
            try
            {
                var user = _userRepository.GetByEmail(input.Email);
                var request = _userService.IssuePasswordReset(user, null, null);

                return Ok();
            }
            catch (Exception e)
            {
                return CreateResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        // GET api/PasswordResetRequest
        public HttpResponseMessage Get(string requestId)
        {
            try
            {
                var request = _userService.GetPasswordReset(requestId);
                if (request == null) return NotFound();
                var dto = AutoMapper.Mapper.Map<PasswordResetRequest, PasswordResetRequestDTO>(request);

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
