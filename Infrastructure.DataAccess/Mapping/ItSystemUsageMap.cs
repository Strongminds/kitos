﻿using Core.DomainModel.ItSystemUsage;

namespace Infrastructure.DataAccess.Mapping
{
    class ItSystemUsageMap : EntityMap<ItSystemUsage>
    {
        public ItSystemUsageMap()
        {
            // Properties
            // Table & Column Mappings
            this.ToTable("ItSystemUsage");

            // Relationships
            HasMany(t => t.ExternalReferences)
                .WithOptional(d => d.ItSystemUsage)
                .HasForeignKey(d => d.ItSystemUsage_Id)
                .WillCascadeOnDelete(true);

            HasOptional(t => t.Reference);

            this.HasRequired(t => t.Organization)
                .WithMany(t => t.ItSystemUsages);

            this.HasMany(t => t.OrgUnits)
                .WithMany(t => t.ItSystemUsages);

            this.HasOptional(t => t.ResponsibleUsage)
                .WithOptionalPrincipal(t => t.ResponsibleItSystemUsage);

            this.HasRequired(t => t.ItSystem)
                .WithMany(t => t.Usages);

            this.HasOptional(t => t.ArchiveType)
                .WithMany(t => t.References)
                .HasForeignKey(d => d.ArchiveTypeId);

            this.HasOptional(t => t.SensitiveDataType)
                .WithMany(t => t.References)
                .HasForeignKey(d => d.SensitiveDataTypeId);

            this.HasOptional(t => t.Overview)
                .WithMany()
                .HasForeignKey(d => d.OverviewId)
                .WillCascadeOnDelete(false);

            this.HasMany(t => t.UsedBy)
                .WithRequired(t => t.ItSystemUsage)
                .HasForeignKey(d => d.ItSystemUsageId)
                .WillCascadeOnDelete(false);

            this.HasOptional(t => t.MainContract)
                .WithOptionalPrincipal()
                .WillCascadeOnDelete(false);

            this.HasMany(t => t.Contracts)
                .WithRequired(t => t.ItSystemUsage)
                .HasForeignKey(d => d.ItSystemUsageId)
                .WillCascadeOnDelete(false);

            this.HasMany(t => t.AccessTypes)
                .WithMany(t => t.ItSystemUsages);

            this.HasOptional(t => t.ArchiveLocation)
                .WithMany(t => t.References)
                .HasForeignKey(d => d.ArchiveLocationId);
        }
    }
}
