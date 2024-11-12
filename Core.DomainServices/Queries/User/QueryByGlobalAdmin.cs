using System.Linq;

namespace Core.DomainServices.Queries.User
{
    public class QueryByGlobalAdmin : IDomainQuery<DomainModel.User>
    {
        public IQueryable<DomainModel.User> Apply(IQueryable<DomainModel.User> source)
        {
            return source.Where(user => user.IsGlobalAdmin);
        }
    }
}
