﻿using System;

namespace Presentation.Web.Models.API.V2.Internal.Request.Notifications
{
    public class UpdateScheduledNotificationWriteRequestDTO : ImmediateNotificationWriteRequestDTO
    {
        /// <summary>
        /// Name of the notification (different from the Subject)
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Date on which the notification expires
        /// </summary>
        public DateTime ToDate { get; set; }
    }
}