using Core.DomainModel.Archive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Mapping
{
    public class ItSystemArchiveMap : EntityMap<ItSystemArchive>
    {
        public override void Configure(EntityTypeBuilder<ItSystemArchive> builder)
        {
            base.Configure(builder);
            builder.ToTable("ItSystemArchive");

            builder.Property(x => x.Uuid).IsRequired();
            builder.HasIndex(x => x.Uuid).IsUnique().HasDatabaseName("UX_ItSystemArchive_Uuid");

            builder.Property(x => x.Note).IsRequired();
            builder.Property(x => x.ArchivingDate).IsRequired();
            builder.Property(x => x.ReferenceName).IsRequired();

            builder.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Snapshot)
                .WithOne(x => x.ItSystemArchive)
                .HasForeignKey<ItSystemUsageArchiveSnapshot>(x => x.ItSystemArchiveUuid)
                .HasPrincipalKey<ItSystemArchive>(x => x.Uuid)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ArchiveReferences)
                .WithOne(x => x.ItSystemArchive)
                .HasForeignKey(x => x.ItSystemArchiveUuid)
                .HasPrincipalKey(x => x.Uuid)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.SnapshotUuid)
                .IsUnique()
                .HasDatabaseName("UX_ItSystemArchive_ItSystemUsageSnapshotUuid");
        }
    }
}
