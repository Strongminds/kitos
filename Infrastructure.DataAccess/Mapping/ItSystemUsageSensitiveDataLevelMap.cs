using Core.DomainModel.ItSystemUsage.GDPR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Mapping
{
    public class ItSystemUsageSensitiveDataLevelMap : IEntityTypeConfiguration<ItSystemUsageSensitiveDataLevel>
    {
        public void Configure(EntityTypeBuilder<ItSystemUsageSensitiveDataLevel> builder)
        {
            // SQL Server rejects CASCADE here due to multiple cascade paths through ItSystemUsage.
            builder
                .HasOne(x => x.ItSystemUsage)
                .WithMany(x => x.SensitiveDataLevels)
                .HasForeignKey("ItSystemUsage_Id")
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
