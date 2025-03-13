using System;

namespace Core.DomainModel.DataReport;

public class ItSystemUsageDataReport : IBaseDataReport
{
    public ItSystemUsageDataReport(DateTime lastChangedAt, int numberOfEntities)
    {
        LastChangedAt = lastChangedAt;
        NumberOfEntities = numberOfEntities;
    }
    public DateTime LastChangedAt { get; set; }
    public int NumberOfEntities { get; set; }
}