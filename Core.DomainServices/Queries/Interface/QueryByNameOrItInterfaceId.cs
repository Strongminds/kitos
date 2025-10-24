using System.Linq;
using Core.DomainModel.ItSystem;

namespace Core.DomainServices.Queries.Interface
{
    public class QueryByNameOrItInterfaceId: IDomainQuery<ItInterface>
    {
        private readonly string _query;

        public QueryByNameOrItInterfaceId(string query)
        {
            _query = query;
        }

        public IQueryable<ItInterface> Apply(IQueryable<ItInterface> source)
        {
            return source.Where(x => x.Name.Contains(_query) || x.ItInterfaceId.Contains(_query));
        }
    }
}
