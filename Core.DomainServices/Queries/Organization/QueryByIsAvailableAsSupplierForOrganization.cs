using System.Linq;

namespace Core.DomainServices.Queries.Organization
{
    public class QueryByIsAvailableAsSupplierForOrganization : IDomainQuery<DomainModel.Organization.Organization>
    {
        private readonly int _query;

        public QueryByIsAvailableAsSupplierForOrganization(int query)
        {
            _query = query;
        }

        public IQueryable<DomainModel.Organization.Organization> Apply(IQueryable<DomainModel.Organization.Organization> source)
        {
            return source.Where(x => !x.UsedAsSupplierByOrganizations.Select(supplier => supplier.OrganizationId).Contains(_query));
        }
    }
}
