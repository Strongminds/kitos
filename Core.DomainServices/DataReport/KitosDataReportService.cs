using System;
using System.Linq;
using Core.DomainModel;
using Core.DomainModel.DataReport;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Organization;
using Core.DomainServices.Extensions;
using Core.DomainServices.Repositories.Organization;

namespace Core.DomainServices.DataReport;

public class KitosDataReportService : IKitosDataReportService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IGenericRepository<ItSystemUsage> _systemUsageRepository;
    private readonly IGenericRepository<ItContract> _itContractRepository;
    private readonly IGenericRepository<DataProcessingRegistration> _dataProcessingRegistrationRepository;
    public KitosDataReportService(IOrganizationRepository organizationRepository, IGenericRepository<ItSystemUsage> usageRepository, IGenericRepository<ItContract> contractRepository, IGenericRepository<DataProcessingRegistration> dprRepository)
    {
        _organizationRepository = organizationRepository;
        _systemUsageRepository = usageRepository;
        _itContractRepository = contractRepository;
        _dataProcessingRegistrationRepository = dprRepository;
    }

    public KitosDataReport GenerateDataReport()
    {
        var organizations = _organizationRepository.GetAll();
        var organizationReports = organizations.AsEnumerable().Select(GenerateOrganizationReport);
        return new KitosDataReport(organizationReports);
    }

    private OrganizationDataReport GenerateOrganizationReport(Organization organization)
    {
        var uuid = organization.Uuid;
        var usageReport = GenerateUsageReport(uuid);
        var contractReport = GenerateContractDataReport(uuid);
        var dprReport = GenerateDataProcessingRegistrationDataReport(uuid);
        return new OrganizationDataReport(organization, usageReport, contractReport, dprReport);
    }

    private ItSystemUsageDataReport GenerateUsageReport(Guid organizationUuid)
    {
        var (count, lastChanged) = GetBaseDataReportFromRepository(_systemUsageRepository, organizationUuid);
        return new ItSystemUsageDataReport(count, lastChanged);
    }

    private ItContractDataReport GenerateContractDataReport(Guid organizationUuid)
    {
        var (count, lastChanged) = GetBaseDataReportFromRepository(_itContractRepository, organizationUuid);
        return new ItContractDataReport(count, lastChanged);
    }

    private DataProcessingRegistrationDataReport GenerateDataProcessingRegistrationDataReport(Guid organizationUuid)
    {
        var (count, lastChanged) = GetBaseDataReportFromRepository(_dataProcessingRegistrationRepository, organizationUuid);
        return new DataProcessingRegistrationDataReport(count, lastChanged);
    }

    private static (DateTime, int) GetBaseDataReportFromRepository<T>(IGenericRepository<T> repository, Guid organizationUuid)
        where T : class, IOwnedByOrganization, IEntity
    {
        var queryable = GetEntitiesByOrganizationUuid(repository, organizationUuid);
        return GetBaseDataReportValues(queryable);
    }

    private static IQueryable<T> GetEntitiesByOrganizationUuid<T>(IGenericRepository<T> repository, Guid organizationUuid) where T : class, IOwnedByOrganization
    {
        return repository.AsQueryable().ByOrganizationUuid(organizationUuid);
    }
    private static (DateTime, int) GetBaseDataReportValues<T>(IQueryable<T> queryable)
    where T : IEntity
    {
        var count = queryable.Count();
        var lastChanged = queryable.Select(x => x.LastChanged).Max();
        return (lastChanged, count);
    }
}