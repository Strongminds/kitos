using Core.ApplicationServices.Users;
using Core.DomainModel.Events;
using System;

namespace Core.ApplicationServices.Authentication
{
    public class UserLoggedInEventHandler(IUserLoggedInService userLoggedInService) : IDomainEventHandler<UserLoggedInEvent>
    {
        public void Handle(UserLoggedInEvent domainEvent)
        {
            var uuid = domainEvent.UserUuid;
            var now = DateTime.UtcNow;
            userLoggedInService.UpdateLatestLoginWithoutAuthenticating(uuid, now);
        }
    }
}
