using Core.DomainModel;
using Core.DomainModel.GDPR.Read;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItContract.Read;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.ItSystemUsage.Read;
using Core.DomainModel.Organization;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace Presentation.Web.Infrastructure.OData
{
    public static class ODataModelConfig
    {
        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();

            RegisterEntitySet<User>(builder, "Users");
            RegisterEntitySet<Organization>(builder, "Organizations");
            RegisterEntitySet<OrganizationUnit>(builder, "OrganizationUnits");
            RegisterEntitySet<OrganizationRight>(builder, "OrganizationRights");
            RegisterEntitySet<ItSystem>(builder, "ItSystems");
            RegisterEntitySet<ItSystemUsage>(builder, "ItSystemUsages");
            RegisterEntitySet<ItSystemRight>(builder, "ItSystemRights");
            RegisterEntitySet<ItInterface>(builder, "ItInterfaces");
            RegisterEntitySet<ItContract>(builder, "ItContracts");
            RegisterEntitySet<ItSystemUsageOverviewReadModel>(builder, "ItSystemUsageOverviewReadModels");
            RegisterEntitySet<ItContractOverviewReadModel>(builder, "ItContractOverviewReadModels");
            RegisterEntitySet<DataProcessingRegistrationReadModel>(builder, "DataProcessingRegistrationReadModels");

            return builder.GetEdmModel();
        }

        private static void RegisterEntitySet<T>(ODataConventionModelBuilder builder, string setName)
            where T : class, IHasId
        {
            builder.EntityType<T>().HasKey(e => e.Id);
            builder.EntitySet<T>(setName);
        }
    }
}
