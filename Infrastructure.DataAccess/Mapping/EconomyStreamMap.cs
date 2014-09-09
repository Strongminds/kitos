using Core.DomainModel.ItContract;

namespace Infrastructure.DataAccess.Mapping
{
    public class EconomyStreamMap : EntityMap<EconomyStream>
    {
        public EconomyStreamMap()
        {
            // Properties
            // Table & Column Mappings
            this.ToTable("EconomyStream");

            // Relationships
            this.HasOptional(t => t.ExternPaymentFor)
                .WithMany(d => d.ExternEconomyStreams)
                .HasForeignKey(t => t.ExternPaymentForId)
                .WillCascadeOnDelete(true);

            this.HasOptional(t => t.InternPaymentFor)
                .WithMany(d => d.InternEconomyStreams)
                .HasForeignKey(t => t.InternPaymentForId)
                .WillCascadeOnDelete(true);

            this.HasOptional(t => t.OrganizationUnit)
                .WithMany(d => d.EconomyStreams)
                .HasForeignKey(t => t.OrganizationUnitId);

        }
    }
}
