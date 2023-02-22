﻿using Core.DomainModel.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Model.Notification.Write;
using Core.DomainModel.Advice;
using Core.DomainServices.Queries;
using Core.ApplicationServices.Model.Notification;

namespace Core.ApplicationServices.Notification
{
    public interface INotificationService
    {
        Result<IQueryable<Advice>, OperationError> GetNotifications(Guid organizationUuid, params IDomainQuery<Advice>[] conditions);
        Result<Advice, OperationError> GetNotificationByUuid(Guid uuid, RelatedEntityType relatedEntityType);
        IEnumerable<AdviceSent> GetNotificationSentByUuid(Guid uuid, RelatedEntityType relatedEntityType);
        Result<Advice, OperationError> CreateImmediateNotification(ImmediateNotificationModificationParameters parameters);
        Result<Advice, OperationError> CreateScheduledNotification(ScheduledNotificationModificationParameters parameters);
        Result<Advice, OperationError> UpdateScheduledNotification(Guid notificationUuid, UpdateScheduledNotificationModificationParameters parameters);
        Result<Advice, OperationError> DeactivateNotification(Guid notificationUuid, RelatedEntityType relatedEntityType);
        Maybe<OperationError> DeleteNotification(Guid notificationUuid, RelatedEntityType relatedEntityType);
        Result<NotificationAccessRights, OperationError> GetAccessRights(Guid notificationUuid, Guid relatedEntityUuid, RelatedEntityType relatedEntityType);
    }
}