﻿using Core.DomainModel.Notification;
using Core.DomainModel.Shared;

using System.Linq;
using Core.Abstractions.Types;

namespace Core.DomainServices.Repositories.Notification
{
    public interface IUserNotificationRepository
    {
        UserNotification Add(UserNotification newNotification);
        Maybe<OperationError> DeleteById(int id);
        Maybe<UserNotification> GetById(int id);
        IQueryable<UserNotification> GetNotificationFromOrganizationByUserId(int organizationId, int userId, RelatedEntityType relatedEntityType);
        IQueryable<UserNotification> GetByRelatedEntityIdAndType(int relatedEntityId, RelatedEntityType relatedEntityType);
    }
}
