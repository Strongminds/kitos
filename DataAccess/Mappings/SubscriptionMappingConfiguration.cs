﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PubSub.Core.Models;

namespace PubSub.DataAccess.Mappings
{
    public class SubscriptionMappingConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.HasKey(x => x.Uuid);

            builder.Property(x => x.Callback)
                .HasConversion(
                    uri => uri.ToString(),
                    str => new Uri(str)
                );
        }
    }
}
