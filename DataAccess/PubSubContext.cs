using Microsoft.EntityFrameworkCore;
using PubSub.Core.Models;

namespace PubSub.DataAccess
{
    public class PubSubContext : DbContext
    {
        public PubSubContext()
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = "Server=.\\SQLEXPRESS;Database=Kitos_PubSub;Trusted_Connection=True;"; //TODO
            optionsBuilder.UseSqlServer(connectionString);
        }

        public DbSet<Subscription> Subscriptions { get; set; }
    }
}
