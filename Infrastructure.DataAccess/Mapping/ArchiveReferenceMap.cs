using Core.DomainModel.Archive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Mapping
{
    public class ArchiveReferenceMap : IEntityTypeConfiguration<ArchiveReference>
    {
        public void Configure(EntityTypeBuilder<ArchiveReference> builder)
        {
            builder.HasKey(x => x.Uuid);
            builder.ToTable("ArchiveReference");

            builder.Property(x => x.Label).IsRequired();
            builder.Property(x => x.Url).IsRequired();

            builder.HasOne(x => x.ItSystemArchive)
                .WithMany(x => x.ArchiveReferences)
                .HasForeignKey(x => x.ItSystemArchiveUuid)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
