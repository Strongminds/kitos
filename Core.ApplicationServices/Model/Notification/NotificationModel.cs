using Core.DomainModel.Advice;
using System;
using System.Collections.Generic;

namespace Core.ApplicationServices.Model.Notification
{
    public class NotificationModel : UpdateNotificationModel //TODO: Split model into immediate and scheduled notification
    {
        public Scheduling? RepetitionFrequency { get; set; }
        public DateTime? FromDate { get; set; }

        public IEnumerable<RecipientModel> Recipients { get; set; } //TODO: Why not RoleRecipients and EmailRecipient.. would enable a distinct Email and Role recipient model for inputs

    }
}
