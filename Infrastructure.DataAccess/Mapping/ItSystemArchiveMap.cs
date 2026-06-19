using Core.DomainModel.Archive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Mapping
{
    public class ItSystemArchiveMap : IEntityTypeConfiguration<ItSystemArchive>
    {
        public void Configure(EntityTypeBuilder<ItSystemArchive> builder)
        {
            builder.HasKey(x => x.Uuid);
            builder.ToTable("ItSystemArchive");

            builder.Property(x => x.Note).IsRequired();
            builder.Property(x => x.ArchivingDate).IsRequired();
            builder.Property(x => x.ESDHName).IsRequired();

            builder.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationUuid)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ItSystemUsageArchiveSnapshot)
                .WithOne(x => x.ItSystemArchive)
                .HasForeignKey<ItSystemUsageArchiveSnapshot>(x => x.ItSystemArchiveUuid)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ArchiveReferences)
                .WithOne(x => x.ItSystemArchive)
                .HasForeignKey(x => x.ItSystemArchiveUuid)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.ItSystemUsageSnapshotUuid)
                .IsUnique()
                .HasDatabaseName("UX_ItSystemArchive_ItSystemUsageSnapshotUuid");
        }
    }
}
