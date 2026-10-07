using System.Linq;

namespace Core.DomainServices.Queries.User;

public class QueryByPubSubUser : IDomainQuery<DomainModel.User>
{
    public IQueryable<DomainModel.User> Apply(IQueryable<DomainModel.User> source) =>
        source.Where(user => user.IsPubSubUser);
}
