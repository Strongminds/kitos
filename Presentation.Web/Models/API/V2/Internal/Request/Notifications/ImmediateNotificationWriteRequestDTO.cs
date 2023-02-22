﻿using System;
using System.ComponentModel.DataAnnotations;

namespace Presentation.Web.Models.API.V2.Internal.Request.Notifications
{
    public class ImmediateNotificationWriteRequestDTO //TODO: Rename to NotificationWriteRequestDTO
    {
        [Required]
        public string Subject { get; set; }
        [Required]
        public string Body { get; set; }
        [Required]
        public Guid OwnerResourceUuid { get; set; } //TODO: remove once resourceowneruuid is a route param

        public RecipientWriteRequestDTO Ccs { get; set; }
        public RecipientWriteRequestDTO Receivers { get; set; }
    }
}