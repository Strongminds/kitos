namespace Core.DomainModel.Events;

public class SystemTakenIntoUsageEvent(ItSystemUsage.ItSystemUsage itSystemUsage) : IDomainEvent
{
    public ItSystemUsage.ItSystemUsage ItSystemUsage { get; set; } = itSystemUsage;
}
