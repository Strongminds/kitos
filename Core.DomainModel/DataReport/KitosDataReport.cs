using System.Collections.Generic;

namespace Core.DomainModel.DataReport;

public class KitosDataReport
{
    public KitosDataReport(IEnumerable<OrganizationDataReport> organizationReports)
    {
        OrganizationReports = organizationReports;
    }

    public IEnumerable<OrganizationDataReport> OrganizationReports { get; set; }
}