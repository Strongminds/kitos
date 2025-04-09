using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PubSub.Core.Models;

namespace PubSub.DataAccess.Mappings
{
    internal class SubscriptionMappingConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            // Configure the Callback property to be stored as a string.
            builder.Property(s => s.Callback)
                .HasConversion(
                    uri => uri.ToString(),
                    str => new Uri(str)
                );

            // Configure the Topics collection as an owned entity.
            builder.Property(x => x.Topic)
                .HasConversion(topic => topic.Name, name => new Topic(name));

        }
    }
}
