using System.Data.Entity.ModelConfiguration;
using Core.DomainModel.Organization;

namespace Infrastructure.DataAccess.Mapping
{
    public class OrganizationSupplierMap : EntityTypeConfiguration<OrganizationSupplier>
    {
        public OrganizationSupplierMap()
        {
            HasKey(x => new
            {
                x.SupplierId,
                x.OrganizationId
            });

            HasRequired(x => x.Supplier)
                .WithMany(x => x.UsedAsSupplierByOrganizations)
                .HasForeignKey(x => x.SupplierId)
                .WillCascadeOnDelete(false);
            HasRequired(x => x.Organization)
                .WithMany(x => x.Suppliers)
                .HasForeignKey(x => x.OrganizationId)
                .WillCascadeOnDelete(true);
        }
    }
}
