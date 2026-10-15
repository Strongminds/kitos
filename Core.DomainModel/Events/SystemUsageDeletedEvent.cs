using System;

namespace Core.DomainModel.Events;

public class SystemUsageDeletedEvent(Guid systemUuid, Guid organizationUuid) : IDomainEvent
{
    public Guid SystemUuid { get; } = systemUuid;
    public Guid OrganizationUuid { get; } = organizationUuid;
}
