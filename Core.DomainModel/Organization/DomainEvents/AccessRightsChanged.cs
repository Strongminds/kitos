﻿using Core.DomainModel.Events;

namespace Core.DomainModel.Organization.DomainEvents
{
    public class AccessRightsChanged : IDomainEvent
    {
        public int UserId { get; }

        public AccessRightsChanged(int userId)
        {
            UserId = userId;
        }
    }
}
