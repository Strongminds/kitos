using System.Collections.Generic;

namespace Core.DomainModel.DataReport;

public class KitosDataReport
{
    public IEnumerable<OrganizationDataReport> OrganizationReports { get; set; }
}