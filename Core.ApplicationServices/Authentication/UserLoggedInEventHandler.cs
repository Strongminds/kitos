using Core.ApplicationServices.Users.Write;
using Core.DomainModel.Events;
using System;

namespace Core.ApplicationServices.Authentication
{
    public class UserLoggedInEventHandler(IUserWriteService userWriteService) : IDomainEventHandler<UserLoggedInEvent>
    {
        public void Handle(UserLoggedInEvent domainEvent)
        {
            var uuid = domainEvent.UserUuid;
            var now = DateTime.UtcNow;
            userWriteService.UpdateLatestLogin(uuid, now);
        }
    }
}
