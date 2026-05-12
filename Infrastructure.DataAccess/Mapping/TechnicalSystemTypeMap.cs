namespace Infrastructure.DataAccess.Mapping
{
    using Core.DomainModel.ItSystem;
    using Core.DomainModel.ItSystemUsage;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class TechnicalSystemTypeMap : OptionEntityMap<TechnicalSystemType, ItSystemUsage>
    {
        public override void Configure(EntityTypeBuilder<TechnicalSystemType> builder)
        {
            base.Configure(builder);
            builder.ToTable("TechnicalSystemTypes");
        }
    }
}
