using System;

namespace Core.DomainModel.DataReport;

public class DataProcessingRegistrationDataReport : IBaseDataReport
{
    public DataProcessingRegistrationDataReport(DateTime lastChangedAt, int numberOfEntities)
    {
        LastChangedAt = lastChangedAt;
        NumberOfEntities = numberOfEntities;
    }
    public DateTime LastChangedAt { get; set; }
    public int NumberOfEntities { get; set; }
}