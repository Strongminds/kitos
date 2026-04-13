using Core.DomainModel.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Mapping
{
    public class StsOrganizationIdentityMap : IEntityTypeConfiguration<StsOrganizationIdentity>
    {
        public void Configure(EntityTypeBuilder<StsOrganizationIdentity> builder)
        {
            builder.HasIndex(x => x.ExternalUuid).IsUnique().HasDatabaseName("UX_" + nameof(StsOrganizationIdentity.ExternalUuid));

            builder.HasOne(x => x.Organization)
                .WithMany(x => x.StsOrganizationIdentities)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
