using Core.DomainModel.Qa.References;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Mapping
{
    public class BrokenLinkInExternalReferenceMap : IEntityTypeConfiguration<BrokenLinkInExternalReference>
    {
        public void Configure(EntityTypeBuilder<BrokenLinkInExternalReference> builder)
        {
            builder.HasOne(x => x.BrokenReferenceOrigin)
                .WithMany(x => x.BrokenLinkReports)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
