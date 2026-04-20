using Core.DomainModel.Qa.References;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Mapping
{
    public class BrokenLinkInInterfaceMap : IEntityTypeConfiguration<BrokenLinkInInterface>
    {
        public void Configure(EntityTypeBuilder<BrokenLinkInInterface> builder)
        {
            builder.ToTable("BrokenLinkInInterfaces");

            builder.HasOne(x => x.BrokenReferenceOrigin)
                .WithMany(x => x.BrokenLinkReports)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
