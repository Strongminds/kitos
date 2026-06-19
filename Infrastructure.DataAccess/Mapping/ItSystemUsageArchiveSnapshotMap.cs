using Core.DomainModel.Archive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Mapping
{
    public class ItSystemUsageArchiveSnapshotMap : IEntityTypeConfiguration<ItSystemUsageArchiveSnapshot>
    {
        public void Configure(EntityTypeBuilder<ItSystemUsageArchiveSnapshot> builder)
        {
            builder.HasKey(x => x.Uuid);
            builder.ToTable("ItSystemUsageArchiveSnapshot");

            builder.Property(x => x.ItSystemArchiveUuid).IsRequired();
            builder.HasIndex(x => x.ItSystemArchiveUuid).IsUnique().HasDatabaseName("UX_ItSystemUsageArchiveSnapshot_ItSystemArchiveUuid");
        }
    }
}
