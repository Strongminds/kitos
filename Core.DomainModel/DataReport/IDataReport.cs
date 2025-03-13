using System;

namespace Core.DomainModel.DataReport;

public interface IBaseDataReport
{
    public DateTime LastChangedAt { get; set; }
    public int NumberOfEntities { get; set; }
}