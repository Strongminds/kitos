using System.Data.Entity;
using System.Linq;

namespace Infrastructure.DataAccess.Tools
{
    public static class NoTrackingQuery
    {
        public static IQueryable<T> AsNoTracking<T>(IQueryable<T> input) where T : class
        {
            return input.AsNoTracking();
        }
    }
}
