﻿using System;
using Core.DomainModel.Advice;
using Core.DomainModel.Shared;

namespace Core.ApplicationServices.Model.Notification.Write
{
    //TODO: Rename to CreateScheduledNotificationModificationParameters
    public class ScheduledNotificationModificationParameters: UpdateScheduledNotificationModificationParameters
    {
        //TODO: consider just accepting a notificationmodificationparameters in stead of inheriting it from the update
        public ScheduledNotificationModificationParameters(string body, string subject, RelatedEntityType type, Guid ownerResourceUuid, RootRecipientModificationParameters ccs, RootRecipientModificationParameters receivers, string name, DateTime? toDate, Scheduling repetitionFrequency, DateTime fromDate) : base(body, subject, type, ownerResourceUuid, ccs, receivers, name, toDate)
        {
            RepetitionFrequency = repetitionFrequency;
            FromDate = fromDate;
        }

        public Scheduling RepetitionFrequency { get; }
        public DateTime FromDate { get; }

    }
}
