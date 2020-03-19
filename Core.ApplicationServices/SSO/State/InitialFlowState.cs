﻿using Core.ApplicationServices.SSO.Model;
using Core.DomainModel.Result;
using Core.DomainServices.SSO;

namespace Core.ApplicationServices.SSO.State
{
    public class InitialFlowState : AbstractState
    {
        private readonly IStsBrugerInfoService _stsBrugerInfoService;
        private readonly string _samlKitosReadAccessRoleIdentifier;
        private readonly Saml20IdentityParser _parser;

        public InitialFlowState(IStsBrugerInfoService stsBrugerInfoService, SsoFlowConfiguration configuration)
        {
            _stsBrugerEmailService = stsBrugerEmailService;
            _parser = Saml20IdentityParser.CreateFromContext();
            _samlKitosReadAccessRoleIdentifier = $"{configuration.PrivilegePrefix}/roles/usersystemrole/readaccess/1";
        }

        public override void Handle(FlowEvent @event, FlowContext context)
        {
            if (@event.Equals(FlowEvent.LoginCompleted))
            {
                var userUuid = GetCurrentUserUuid();
                if (userUuid.HasValue && CurrentUserHasKitosPrivilege())
                {
                    var stsBrugerInfo = _stsBrugerInfoService.GetStsBrugerInfo(userUuid);
                    context.TransitionTo(new LookupStsUserEmailState(stsBrugerInfo.Emails));
                    context.HandleUserHasValidAccessRoleInSamlToken();
                }
                else
                {
                    context.TransitionTo(new UserWithNoPrivilegesState());
                }
            }
        }

        private Maybe<string> GetCurrentUserUuid()
        {
            return _parser.MatchUuid().Select(uuid => uuid.Value.ToString());
        }

        private bool CurrentUserHasKitosPrivilege()
        {
            return _parser
                .MatchPrivilege(_samlKitosReadAccessRoleIdentifier)
                .HasValue;
        }
    }
}