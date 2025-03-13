using Core.DomainModel.DataReport;

namespace Core.DomainServices.DataReport;

public interface IKitosDataReportService
{
    KitosDataReport GenerateDataReport();
}