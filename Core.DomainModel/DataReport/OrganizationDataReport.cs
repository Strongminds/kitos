namespace Core.DomainModel.DataReport;

public class OrganizationDataReport
{
    public Organization.Organization Organization { get; set; }

    public ItSystemUsageDataReport UsageReport { get; set; }

    public ItContractDataReport ContractReport { get; set; }

    public DataProcessingRegistrationDataReport DataProcessingReport { get; set; }

}