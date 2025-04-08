using Microsoft.EntityFrameworkCore;
using PubSub.Core.Models;

namespace PubSub.DataAccess
{
    public class PubSubContext : DbContext
    {
        public PubSubContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options) => throw new NotImplementedException();

        public DbSet<Subscription> Subscriptions { get; set; }
    }
}
