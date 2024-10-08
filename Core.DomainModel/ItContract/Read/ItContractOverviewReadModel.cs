﻿using System;
using System.Collections.Generic;
using Core.DomainModel.Shared;

namespace Core.DomainModel.ItContract.Read
{
    public class ItContractOverviewReadModel : IOwnedByOrganization, IReadModel<ItContract>, IHasName
    {
        public ItContractOverviewReadModel()
        {
            ItSystemUsages = new List<ItContractOverviewReadModelItSystemUsage>();
            DataProcessingAgreements = new List<ItContractOverviewReadModelDataProcessingAgreement>();
            RoleAssignments = new List<ItContractOverviewRoleAssignmentReadModel>();
            SystemRelations = new List<ItContractOverviewReadModelSystemRelation>();
        }

        public int OrganizationId { get; set; }
        public virtual Organization.Organization Organization { get; set; }
        public int Id { get; set; }
        public int SourceEntityId { get; set; }
        public Guid SourceEntityUuid { get; set; }
        public virtual ItContract SourceEntity { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public string ContractId { get; set; }
        public int? ParentContractId { get; set; } //for linking
        public string ParentContractName { get; set; } //for presentation
        public Guid? ParentContractUuid { get; set; }
        public int? CriticalityId { get; set; }     // For filtering
        public Guid? CriticalityUuid { get; set; }     // For filtering
        public string CriticalityName { get; set; } // For sorting
        public int? ResponsibleOrgUnitId { get; set; }     // For filtering
        public string ResponsibleOrgUnitName { get; set; } // For sorting
        public int? SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string ContractSigner { get; set; }
        public int? ContractTypeId { get; set; }     // For filtering
        public Guid? ContractTypeUuid { get; set; }     // For filtering
        public string ContractTypeName { get; set; } // For sorting
        public int? ContractTemplateId { get; set; }     // For filtering
        public Guid? ContractTemplateUuid { get; set; }     // For filtering
        public string ContractTemplateName { get; set; } // For sorting
        public int? PurchaseFormId { get; set; }     // For filtering
        public Guid? PurchaseFormUuid { get; set; }     // For filtering
        public string PurchaseFormName { get; set; } // For sorting
        public int? ProcurementStrategyId { get; set; }     // For filtering
        public Guid? ProcurementStrategyUuid { get; set; }     // For filtering
        public string ProcurementStrategyName { get; set; } // For sorting
        public int? ProcurementPlanYear { get; set; }
        public int? ProcurementPlanQuarter { get; set; }
        public YesNoUndecidedOption? ProcurementInitiated { get; set; }
        public virtual ICollection<ItContractOverviewRoleAssignmentReadModel> RoleAssignments { get; set; }
        public virtual ICollection<ItContractOverviewReadModelDataProcessingAgreement> DataProcessingAgreements { get; set; } //used for generating links and filtering IN collection (we can add index since the name can be constrained)
        public string DataProcessingAgreementsCsv { get; set; } //Used for sorting AND excel output (not filtering since we cannot set a ceiling on length and hence no index)
        public virtual ICollection<ItContractOverviewReadModelItSystemUsage> ItSystemUsages { get; set; } //used for generating links and filtering IN collection (we can add index since the name can be constrained)
        public string ItSystemUsagesCsv { get; set; } //Used for sorting AND excel output 
        public string ItSystemUsagesSystemUuidCsv { get; set; } //Used for sorting AND excel output 
        public int NumberOfAssociatedSystemRelations { get; set; } //for display, order and filtering
        public virtual ICollection<ItContractOverviewReadModelSystemRelation> SystemRelations { get; set; } //for lookup used during update scheduling (to check if an outside change affects this overview item)
        public string ActiveReferenceTitle { get; set; }
        public string ActiveReferenceUrl { get; set; }
        public string ActiveReferenceExternalReferenceId { get; set; }
        public int AccumulatedAcquisitionCost { get; set; }
        public int AccumulatedOperationCost { get; set; }
        public int AccumulatedOtherCost { get; set; }
        public DateTime? OperationRemunerationBegunDate { get; set; }
        public int? PaymentModelId { get; set; }     // For filtering
        public Guid? PaymentModelUuid { get; set; }     // For filtering
        public string PaymentModelName { get; set; } // For sorting
        public int? PaymentFrequencyId { get; set; }     // For filtering
        public Guid? PaymentFrequencyUuid { get; set; }     // For filtering
        public string PaymentFrequencyName { get; set; } // For sorting
        public DateTime? LatestAuditDate { get; set; }
        public int AuditStatusWhite { get; set; }
        public int AuditStatusRed { get; set; }
        public int AuditStatusYellow { get; set; }
        public int AuditStatusGreen { get; set; }
        public string Duration { get; set; }
        public int? OptionExtendId { get; set; }     // For filtering
        public Guid? OptionExtendUuid { get; set; }     // For filtering
        public string OptionExtendName { get; set; } // For sorting
        public int? TerminationDeadlineId { get; set; }     // For filtering
        public Guid? TerminationDeadlineUuid { get; set; }     // For filtering
        public string TerminationDeadlineName { get; set; } // For sorting
        public DateTime? IrrevocableTo { get; set; }
        public DateTime? TerminatedAt { get; set; }
        public int? LastEditedByUserId { get; set; }
        public string LastEditedByUserName { get; set; }
        public DateTime? LastEditedAtDate { get; set; }
        public DateTime? Concluded { get; set; }
        public DateTime? ExpirationDate { get; set; }

    }
}
