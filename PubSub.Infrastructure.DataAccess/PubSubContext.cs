using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PubSub.Core.DomainModel.Subscriptions;

namespace PubSub.Infrastructure.DataAccess
{
    public class PubSubContext : DbContext
    {
        public PubSubContext(DbContextOptions<PubSubContext> options) : base(options)
        {
        }

        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<PubSub.Core.DomainModel.UserSync.UserChangeDelivery> UserChangeDeliveries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new Mappings.SubscriptionMappingConfiguration());
            modelBuilder.Entity<PubSub.Core.DomainModel.UserSync.UserChangeDelivery>(b =>
            {
                b.HasKey(x => x.Uuid);
                b.Property(x => x.ExternalMessageId).HasMaxLength(200).IsRequired();
                b.HasIndex(x => x.ExternalMessageId).IsUnique();
                b.HasIndex(x => new { x.DeadLettered, x.DeliveredAt, x.NextAttemptAt });
                b.Property(x => x.Version).IsConcurrencyToken();
            });

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var ignorePendingModelChangesWarning =
                string.Equals(Environment.GetEnvironmentVariable("IgnorePendingModelChangesWarning"), "true", StringComparison.OrdinalIgnoreCase);

            if (ignorePendingModelChangesWarning)
            {
                optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            }

            base.OnConfiguring(optionsBuilder);
        }
    }
}
