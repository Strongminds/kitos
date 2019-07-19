﻿using System;
using System.Security.Claims;
using IdentityServer3.Core.Extensions;
using Microsoft.Owin;
using Presentation.Web.Infrastructure.Model.Authentication;
using Serilog;

namespace Presentation.Web.Infrastructure.Factories.Authentication
{
    public class AuthenticationContextFactory : IAuthenticationContextFactory
    {
        private readonly ILogger _logger;
        private readonly IOwinContext _owinContext;

        public AuthenticationContextFactory(ILogger logger, IOwinContext owinContext)
        {
            _logger = logger;
            _owinContext = owinContext;
        }

        public IAuthenticationContext Create()
        {
            var user = _owinContext.Authentication.User;
            return IsAuthenticated(user)
                ? new AuthenticationContext(MapAuthenticationMethod(user), MapUserId(user), MapOrganizationId(user))
                : new AuthenticationContext(AuthenticationMethod.Anonymous);
        }

        private int? MapOrganizationId(ClaimsPrincipal user)
        {
            //TODO JMO
            throw new NotImplementedException();
        }

        private int? MapUserId(ClaimsPrincipal user)
        {
            //TODO JMO
            throw new NotImplementedException();
        }

        private AuthenticationMethod MapAuthenticationMethod(ClaimsPrincipal user)
        {
            var authenticationMethod = user.GetAuthenticationMethod();
            switch (authenticationMethod)
            {
                case "JWT":
                    return AuthenticationMethod.KitosToken;
                    break;
                case "Forms": //TODO JMO
                    throw new NotImplementedException();
                    break;
                default:
                    _logger.Error("Unknown authentication method {authenticationMethod}", authenticationMethod);
                    return AuthenticationMethod.Anonymous;
            }
        }

        private bool IsAuthenticated(ClaimsPrincipal user)
        {
            throw new NotImplementedException();
        }
    }
}