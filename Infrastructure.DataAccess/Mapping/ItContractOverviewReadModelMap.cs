﻿using System;
using System.Data.Entity.ModelConfiguration;
using System.Linq.Expressions;
using Core.DomainModel;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItContract.Read;
using Core.DomainModel.Organization;
using Core.DomainModel.Users;

namespace Infrastructure.DataAccess.Mapping
{
    public class ItContractOverviewReadModelMap : EntityTypeConfiguration<ItContractOverviewReadModel>
    {
        public ItContractOverviewReadModelMap()
        {
            HasRequired(t => t.Organization)
                .WithMany(t => t.ItContractOverviewReadModels)
                .HasForeignKey(d => d.OrganizationId)
                .WillCascadeOnDelete(false);

            HasRequired(x => x.SourceEntity)
                .WithMany(x => x.OverviewReadModels)
                .HasForeignKey(x => x.SourceEntityId)
                .WillCascadeOnDelete(true);

            Property(x => x.SourceEntityUuid).IsRequired();

            Property(x => x.Name)
                .HasMaxLength(ItContractConstraints.MaxNameLength)
                .HasIndexAnnotation("IX_Contract_Name");

            Property(x => x.IsActive)
                .HasIndexAnnotation("IX_Contract_Active");

            Property(x => x.ParentContractId)
                .IsOptional()
                .HasIndexAnnotation("IX_ParentContract_Id");

            Property(x => x.ParentContractName)
                .IsOptional()
                .HasMaxLength(ItContractConstraints.MaxNameLength)
                .HasIndexAnnotation("IX_ParentContract_Name");

            Property(x => x.ParentContractUuid)
                .IsOptional()
                .HasIndexAnnotation("IX_ParentContract_Uuid");

            MapOptionTypeReference<CriticalityType>(p => p.CriticalityId, p => p.CriticalityName, p => p.CriticalityUuid);

            Property(x => x.ResponsibleOrgUnitId)
                .IsOptional()
                .HasIndexAnnotation("IX_ResponsibleOrgUnitId");

            Property(x => x.ResponsibleOrgUnitName)
                .IsOptional();

            Property(x => x.SupplierId)
                .IsOptional()
                .HasIndexAnnotation("IX_SupplierId");

            Property(x => x.SupplierName)
                .HasMaxLength(Organization.MaxNameLength)
                .IsOptional()
                .HasIndexAnnotation("IX_SupplierName");

            Property(x => x.ContractSigner)
                .IsOptional();

            MapOptionTypeReference<ItContractType>(p => p.ContractTypeId, p => p.ContractTypeName, p => p.ContractTypeUuid);

            MapOptionTypeReference<ItContractTemplateType>(p => p.ContractTemplateId, p => p.ContractTemplateName, p => p.ContractTemplateUuid);

            MapOptionTypeReference<PurchaseFormType>(p => p.PurchaseFormId, p => p.PurchaseFormName, p => p.PurchaseFormUuid);

            MapOptionTypeReference<ProcurementStrategyType>(p => p.ProcurementStrategyId, p => p.ProcurementStrategyName, p => p.ProcurementStrategyUuid);

            Property(x => x.ProcurementPlanYear)
                .IsOptional()
                .HasIndexAnnotation("IX_ProcurementPlanYear");

            Property(x => x.ProcurementPlanQuarter)
                .IsOptional()
                .HasIndexAnnotation("IX_ProcurementPlanQuarter");

            Property(x => x.ProcurementInitiated)
                .IsOptional()
                .HasIndexAnnotation("IX_ProcurementInitiated");

            HasMany(x => x.RoleAssignments)
                .WithRequired(x => x.Parent)
                .HasForeignKey(x => x.ParentId)
                .WillCascadeOnDelete(true);

            HasMany(x => x.DataProcessingAgreements)
                .WithRequired(x => x.Parent)
                .HasForeignKey(x => x.ParentId)
                .WillCascadeOnDelete(true);

            HasMany(x => x.SystemRelations)
                .WithRequired(x => x.Parent)
                .HasForeignKey(x => x.ParentId)
                .WillCascadeOnDelete(true);

            Property(x => x.DataProcessingAgreementsCsv)
                .IsOptional();

            HasMany(x => x.ItSystemUsages)
                .WithRequired(x => x.Parent)
                .HasForeignKey(x => x.ParentId)
                .WillCascadeOnDelete(true);

            Property(x => x.ItSystemUsagesCsv)
                .IsOptional();

            Property(x => x.ItSystemUsagesSystemUuidCsv)
                .IsOptional();

            Property(x => x.NumberOfAssociatedSystemRelations)
                .HasIndexAnnotation("IX_NumberOfAssociatedSystemRelations");

            Property(x => x.ActiveReferenceTitle)
                .IsOptional();

            Property(x => x.ActiveReferenceUrl)
                .IsOptional();

            Property(x => x.ActiveReferenceExternalReferenceId)
                .IsOptional();

            Property(x => x.AccumulatedAcquisitionCost)
                .HasIndexAnnotation("IX_AccumulatedAcquisitionCost");

            Property(x => x.AccumulatedOperationCost)
                .HasIndexAnnotation("IX_AccumulatedOperationCost");

            Property(x => x.AccumulatedOtherCost)
                .HasIndexAnnotation("IX_AccumulatedOtherCost");

            Property(x => x.OperationRemunerationBegunDate)
                .IsOptional()
                .HasIndexAnnotation("IX_OperationRemunerationBegunDate");

            MapOptionTypeReference<PaymentModelType>(p => p.PaymentModelId, p => p.PaymentModelName, p => p.PaymentModelUuid);

            MapOptionTypeReference<PaymentFreqencyType>(p => p.PaymentFrequencyId, p => p.PaymentFrequencyName, p => p.PaymentFrequencyUuid);

            Property(x => x.LatestAuditDate)
                .IsOptional()
                .HasIndexAnnotation("IX_LatestAuditDate");

            Property(x => x.AuditStatusWhite);

            Property(x => x.AuditStatusRed);

            Property(x => x.AuditStatusYellow);

            Property(x => x.AuditStatusGreen);

            Property(x => x.Duration)
                .IsOptional()
                .HasMaxLength(100)
                .HasIndexAnnotation("IX_Duration");

            MapOptionTypeReference<OptionExtendType>(p => p.OptionExtendId, p => p.OptionExtendName, p => p.OptionExtendUuid);

            MapOptionTypeReference<TerminationDeadlineType>(p => p.TerminationDeadlineId, p => p.TerminationDeadlineName, p => p.TerminationDeadlineUuid);

            Property(x => x.IrrevocableTo)
                .IsOptional()
                .HasIndexAnnotation("IX_IrrevocableTo");

            Property(x => x.TerminatedAt)
                .IsOptional()
                .HasIndexAnnotation("IX_TerminatedAt");

            Property(x => x.LastEditedByUserName)
                .HasMaxLength(UserConstraints.MaxNameLength)
                .IsOptional()
                .HasIndexAnnotation("IX_LastEditedByUserName");

            Property(x => x.LastEditedByUserId)
                .IsOptional()
                .HasIndexAnnotation("IX_LastEditedByUserId");

            Property(x => x.LastEditedAtDate)
                .IsOptional()
                .HasIndexAnnotation("IX_LastEditedAtDate");

            Property(x => x.Concluded)
                .IsOptional()
                .HasIndexAnnotation("IX_Concluded");

            Property(x => x.ExpirationDate)
                .IsOptional()
                .HasIndexAnnotation("IX_ExpirationDate");
        }

        private void MapOptionTypeReference<T>(Expression<Func<ItContractOverviewReadModel, int?>> idExpression, Expression<Func<ItContractOverviewReadModel, string>> nameExpression, Expression<Func<ItContractOverviewReadModel, Guid?>> uuidExpression)
        {
            Property(idExpression)
                .IsOptional()
                .HasIndexAnnotation($"IX_{typeof(T).Name}_Id");
            Property(nameExpression)
                .IsOptional()
                .HasMaxLength(OptionEntity<T>.MaxNameLength)
                .HasIndexAnnotation($"IX_{typeof(T).Name}_Name");
            Property(uuidExpression)
                .IsOptional()
                .HasIndexAnnotation($"IX_{typeof(T).Name}_Uuid");
        }
    }
}