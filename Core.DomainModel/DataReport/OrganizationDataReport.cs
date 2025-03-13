namespace Core.DomainModel.DataReport;

public class OrganizationDataReport
{
    public OrganizationDataReport(Organization.Organization organization, ItSystemUsageDataReport usageReport, ItContractDataReport contractReport, DataProcessingRegistrationDataReport dprReport )
    {
        Organization = organization;
        UsageReport = usageReport;
        ContractReport = contractReport;
        DataProcessingReport = dprReport;
    }
    public Organization.Organization Organization { get; set; }

    public ItSystemUsageDataReport UsageReport { get; set; }

    public ItContractDataReport ContractReport { get; set; }

    public DataProcessingRegistrationDataReport DataProcessingReport { get; set; }

}