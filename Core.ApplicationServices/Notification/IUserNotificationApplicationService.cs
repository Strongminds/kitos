﻿using Core.DomainModel.Notification;
using Core.DomainModel.Shared;

using System.Linq;
using Core.Abstractions.Types;

namespace Core.ApplicationServices.Notification
{
    public interface IUserNotificationApplicationService
    {
        public Maybe<OperationError> Delete(int id);
        public Result<IQueryable<UserNotification>, OperationError> GetNotificationsForUser(int organizationId, int userId, RelatedEntityType relatedEntityType);
    }
}
