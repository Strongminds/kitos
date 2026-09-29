using Core.DomainModel.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Mapping;

public class ExternalUserChangeMap : IEntityTypeConfiguration<ExternalUserChange>
{
    public void Configure(EntityTypeBuilder<ExternalUserChange> builder)
    {
        builder.ToTable("ExternalUserChanges");
        builder.HasKey(x => x.Uuid);
        builder.Property(x => x.ExternalMessageId).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.ExternalMessageId).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.Status, x.ReceivedAt });
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ResolvedByUser).WithMany().HasForeignKey(x => x.ResolvedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
