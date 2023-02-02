﻿using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Model.Messages;

namespace Core.ApplicationServices.Messages
{
    public interface IPublicMessagesService
    {
        ResourcePermissionsResult GetPermissions();
        PublicMessages GetPublicMessages();
        Result<PublicMessages, OperationError> UpdateMessages(WritePublicMessagesParams parameters);
    }
}
