using System.Data.Entity.ModelConfiguration;
using Core.DomainModel.ItContract;

namespace Infrastructure.DataAccess.Mapping
{
    public class ItContractMap : EntityTypeConfiguration<ItContract>
    {
        public ItContractMap()
        {
            // Properties

            this.Property(x => x.Name)
                .HasMaxLength(ItContractConstraints.MaxNameLength)
                .IsRequired();

            HasIndex(x => new { x.OrganizationId, x.Name })
                .IsUnique(true)
                .HasName("UX_NameUniqueToOrg");

            HasIndex(x => x.OrganizationId)
                .IsUnique(false)
                .HasName("IX_OrganizationId");

            HasIndex(x => x.Name)
                .IsUnique(false)
                .HasName("IX_Name");

            HasIndex(x => x.ProcurementInitiated)
                .IsUnique(false)
                .HasName("IX_ProcurementInitiated");

            // Table & Column Mappings
            ToTable("ItContract");

            HasOptional(t => t.Reference);
            HasMany(t => t.ExternalReferences)
                .WithOptional(d => d.ItContract)
                .HasForeignKey(d => d.Itcontract_Id)
                .WillCascadeOnDelete(true);

            HasOptional(t => t.ContractTemplate)
                .WithMany(t => t.References)
                .HasForeignKey(d => d.ContractTemplateId);

            HasOptional(t => t.ContractType)
                .WithMany(t => t.References)
                .HasForeignKey(d => d.ContractTypeId);

            HasOptional(t => t.PurchaseForm)
                .WithMany(t => t.References)
                .HasForeignKey(d => d.PurchaseFormId);

            HasOptional(t => t.Supplier)
                .WithMany(t => t.Supplier)
                .HasForeignKey(d => d.SupplierId);

            HasOptional(t => t.ProcurementStrategy)
                .WithMany(t => t.References)
                .HasForeignKey(d => d.ProcurementStrategyId);

            HasOptional(t => t.Parent)
                .WithMany(t => t.Children)
                .HasForeignKey(d => d.ParentId)
                .WillCascadeOnDelete(false);

            HasOptional(t => t.Criticality)
                .WithMany(t => t.References)
                .HasForeignKey(d => d.CriticalityId);

            HasMany(t => t.AssociatedAgreementElementTypes)
               .WithRequired(t => t.ItContract)
               .HasForeignKey(t=> t.ItContract_Id);

            HasOptional(t => t.ResponsibleOrganizationUnit)
                .WithMany(t => t.ResponsibleForItContracts)
                .HasForeignKey(d => d.ResponsibleOrganizationUnitId);

            HasMany(t => t.AssociatedSystemUsages)
                .WithRequired(t => t.ItContract)
                .HasForeignKey(d => d.ItContractId);

            HasRequired(t => t.Organization)
                .WithMany(t => t.ItContracts)
                .HasForeignKey(d => d.OrganizationId)
                .WillCascadeOnDelete(false);

            HasMany(x => x.DataProcessingRegistrations)
                .WithMany(x => x.AssociatedContracts);

            HasMany(t => t.AssociatedDataProcessingRegistrations)
                .WithRequired(t => t.ItContract)
                .HasForeignKey(d => d.ItContractId);

            Property(x => x.Uuid)
                .IsRequired()
                .HasUniqueIndexAnnotation("UX_Contract_Uuid", 0);
        }
    }
}