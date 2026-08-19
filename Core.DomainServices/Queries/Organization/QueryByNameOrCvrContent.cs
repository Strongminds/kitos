using System;
using System.Linq;

namespace Core.DomainServices.Queries.Organization
{
    public class QueryByNameOrCvrContent : IDomainQuery<DomainModel.Organization.Organization>
    {
        private readonly string _query;
        private readonly string _queryLower;

        public QueryByNameOrCvrContent(string query)
        {
            _query = query;
            _queryLower = query?.ToLower();
        }

        public IQueryable<DomainModel.Organization.Organization> Apply(IQueryable<DomainModel.Organization.Organization> source)
        {
            return source.Where(x => x.Cvr != null && x.Cvr.ToLower().Contains(_queryLower) || x.Name.ToLower().Contains(_queryLower));
        }
    }
}
