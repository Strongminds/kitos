using System;
using System.Linq;
using Core.DomainModel.ItSystem;

namespace Core.DomainServices.Queries.Interface
{
    public class QueryByNameOrItInterfaceId: IDomainQuery<ItInterface>
    {
        private readonly string _query;
        private readonly string _queryLower;

        public QueryByNameOrItInterfaceId(string query)
        {
            _query = query;
            _queryLower = query?.ToLower();
        }

        public IQueryable<ItInterface> Apply(IQueryable<ItInterface> source)
        {
            return source.Where(x => x.Name.ToLower().Contains(_queryLower) || x.ItInterfaceId.ToLower().Contains(_queryLower));
        }
    }
}
