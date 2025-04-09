using Microsoft.EntityFrameworkCore;
using PubSub.Core.Models;

namespace PubSub.DataAccess
{
    public class PubSubContext : DbContext
    {
        public PubSubContext(DbContextOptions<PubSubContext> options) : base(options)
        {
        }

        public DbSet<Subscription> Subscriptions { get; set; }


    }
}
