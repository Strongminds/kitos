using Core.DomainModel.ItSystemUsage.GDPR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.Mapping
{
    public class ItSystemUsageSensitiveDataLevelMap : IEntityTypeConfiguration<ItSystemUsageSensitiveDataLevel>
    {
        public void Configure(EntityTypeBuilder<ItSystemUsageSensitiveDataLevel> builder)
        {
        }
    }
}
