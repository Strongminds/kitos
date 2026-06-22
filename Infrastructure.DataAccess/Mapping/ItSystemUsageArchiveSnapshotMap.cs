using Core.DomainModel.Archive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Mapping
{
    public class ItSystemUsageArchiveSnapshotMap : EntityMap<ItSystemUsageArchiveSnapshot>
    {
        public override void Configure(EntityTypeBuilder<ItSystemUsageArchiveSnapshot> builder)
        {
            base.Configure(builder);
            builder.ToTable("Snapshot");

            builder.Property(x => x.Uuid).IsRequired();
            builder.HasIndex(x => x.Uuid).IsUnique().HasDatabaseName("UX_ItSystemUsageArchiveSnapshot_Uuid");

            builder.Property(x => x.ItSystemArchiveUuid).IsRequired();
            builder.HasIndex(x => x.ItSystemArchiveUuid).IsUnique().HasDatabaseName("UX_ItSystemUsageArchiveSnapshot_ItSystemArchiveUuid");

            builder.Property(x => x.ItSystemUuid).IsRequired();
            builder.HasIndex(x => x.ItSystemUuid).HasDatabaseName("IX_ItSystemUsageArchiveSnapshot_ItSystemUuid");

            builder.HasOne(x => x.ItSystem)
                .WithMany()
                .HasForeignKey(x => x.ItSystemUuid)
                .HasPrincipalKey(x => x.Uuid)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
