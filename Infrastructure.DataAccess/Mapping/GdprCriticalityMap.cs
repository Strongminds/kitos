namespace Infrastructure.DataAccess.Mapping
{
    using Core.DomainModel.ItSystem;
    using Core.DomainModel.ItSystemUsage;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class GdprCriticalityMap : OptionEntityMap<GdprCriticality, ItSystemUsage>
    {
        public override void Configure(EntityTypeBuilder<GdprCriticality> builder)
        {
            base.Configure(builder);
            builder.ToTable("GdprCriticalityTypes");
        }
    }
}
