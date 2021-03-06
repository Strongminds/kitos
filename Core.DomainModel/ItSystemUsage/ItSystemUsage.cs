﻿using System.Collections.Generic;
using System.Linq;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage.GDPR;
using Core.DomainModel.Organization;
using Core.DomainModel.References;
using Core.DomainModel.Result;
using Core.DomainModel.Shared;
using Infrastructure.Services.Types;

namespace Core.DomainModel.ItSystemUsage
{
    using ItSystem.DataTypes;
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    using ItSystem = Core.DomainModel.ItSystem.ItSystem;

    /// <summary>
    /// Represents an organisation's usage of an it system.
    /// </summary>
    public class ItSystemUsage : HasRightsEntity<ItSystemUsage, ItSystemRight, ItSystemRole>, ISystemModule, IOwnedByOrganization, IEntityWithExternalReferences, IHasAttachedOptions
    {
        public ItSystemUsage()
        {
            this.Contracts = new List<ItContractItSystemUsage>();
            this.ArchivePeriods = new List<ArchivePeriod>();
            this.OrgUnits = new List<OrganizationUnit>();
            this.TaskRefs = new List<TaskRef>();
            this.AccessTypes = new List<AccessType>();
            this.TaskRefsOptOut = new List<TaskRef>();
            this.UsedBy = new List<ItSystemUsageOrgUnitUsage>();
            this.ItProjects = new List<ItProject.ItProject>();
            ExternalReferences = new List<ExternalReference>();
            UsageRelations = new List<SystemRelation>();
            UsedByRelations = new List<SystemRelation>();
            SensitiveDataLevels = new List<ItSystemUsageSensitiveDataLevel>();
        }

        public bool IsActive
        {
            get
            {
                if (!this.Active)
                {
                    var today = DateTime.UtcNow;
                    var startDate = this.Concluded ?? today;
                    var endDate = DateTime.MaxValue;

                    if (ExpirationDate.HasValue && ExpirationDate.Value != DateTime.MaxValue)
                    {
                        endDate = ExpirationDate.Value.Date.AddDays(1).AddTicks(-1);
                    }

                    if (this.Terminated.HasValue)
                    {
                        var terminationDate = this.Terminated;
                        if (this.TerminationDeadlineInSystem != null)
                        {
                            int deadline;
                            int.TryParse(this.TerminationDeadlineInSystem.Name, out deadline);
                            terminationDate = this.Terminated.Value.AddMonths(deadline);
                        }
                        // indgået-dato <= dags dato <= opsagt-dato + opsigelsesfrist
                        return today >= startDate.Date && today <= terminationDate.Value.Date.AddDays(1).AddTicks(-1);
                    }

                    // indgået-dato <= dags dato <= udløbs-dato
                    return today >= startDate.Date && today <= endDate;
                }
                return this.Active;
            }
        }

        /// <summary>
        ///     Gets or sets Active.
        /// </summary>
        /// <value>
        ///   Active.
        /// </value>
        public bool Active { get; set; }

        /// <summary>
        ///     When the system began. (indgået)
        /// </summary>
        /// <value>
        ///     The concluded date.
        /// </value>
        public DateTime? Concluded { get; set; }

        /// <summary>
        ///     When the system expires. (udløbet)
        /// </summary>
        /// <value>
        ///     The expiration date.
        /// </value>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        ///     Date the system ends. (opsagt)
        /// </summary>
        /// <value>
        ///     The termination date.
        /// </value>
        public DateTime? Terminated { get; set; }

        /// <summary>
        ///     Gets or sets the termination deadline option. (opsigelsesfrist)
        /// </summary>
        /// <remarks>
        ///     Added months to the <see cref="Terminated" /> contract termination date before the contract expires.
        ///     It's a string but should be treated as an int.
        /// </remarks>
        /// <value>
        ///     The termination deadline.
        /// </value>
        public virtual TerminationDeadlineTypesInSystem TerminationDeadlineInSystem { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance's status is active.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance's status is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsStatusActive { get; set; }
        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        /// <value>
        /// The note.
        /// </value>
        public string Note { get; set; }
        /// <summary>
        /// Gets or sets the user defined local system identifier.
        /// </summary>
        /// <remarks>
        /// This identifier is not the primary key.
        /// </remarks>
        /// <value>
        /// The local system identifier.
        /// </value>
        public string LocalSystemId { get; set; }
        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public string Version { get; set; }
        /// <summary>
        /// Gets or sets a reference to relevant documents in an extern ESDH system.
        /// </summary>
        /// <value>
        /// Extern reference  to ESDH system.
        /// </value>
        public string EsdhRef { get; set; }
        /// <summary>
        /// Gets or sets a reference to relevant documents in an extern CMDB system.
        /// </summary>
        /// <value>
        /// Extern reference  to CMDB system.
        /// </value>
        public string CmdbRef { get; set; }
        /// <summary>
        /// Gets or sets a path or url to relevant documents.
        /// </summary>
        /// <value>
        /// Path or url relevant documents.
        /// </value>
        public string DirectoryOrUrlRef { get; set; }
        /// <summary>
        /// Gets or sets the local call system.
        /// </summary>
        /// <value>
        /// The local call system.
        /// </value>
        public string LocalCallName { get; set; }
        /// <summary>
        /// Organization Unit responsible for this system usage.
        /// </summary>
        /// <value>
        /// The responsible organization unit.
        /// </value>
        public virtual ItSystemUsageOrgUnitUsage ResponsibleUsage { get; set; }

        public int OrganizationId { get; set; }
        /// <summary>
        /// Gets or sets the organization marked as responsible for this it system usage.
        /// </summary>
        /// <value>
        /// The responsible organization.
        /// </value>
        public virtual Organization.Organization Organization { get; set; }

        public int ItSystemId { get; set; }
        /// <summary>
        /// Gets or sets the it system this instance is using.
        /// </summary>
        /// <value>
        /// It system.
        /// </value>
        public virtual ItSystem ItSystem { get; set; }

        public int? ArchiveTypeId { get; set; }
        public virtual ArchiveType ArchiveType { get; set; }

        public int? SensitiveDataTypeId { get; set; }
        public virtual SensitiveDataType SensitiveDataType { get; set; }

        public int? OverviewId { get; set; }
        /// <summary>
        /// Gets or sets the it system usage that is set to be displayed on the it system overview page.
        /// </summary>
        /// <remarks>
        /// It's the it system name that is actually displayed.
        /// </remarks>
        /// <value>
        /// The overview it system.
        /// </value>
        public virtual ItSystemUsage Overview { get; set; }

        /// <summary>
        /// Gets or sets the main it contract for this instance.
        /// The it contract is used to determine whether this instance
        /// is marked as active/inactive.
        /// </summary>
        /// <value>
        /// The main contract.
        /// </value>
        public virtual ItContractItSystemUsage MainContract { get; set; }
        /// <summary>
        /// Gets or sets it contracts associated with this instance.
        /// </summary>
        /// <value>
        /// The contracts.
        /// </value>
        public virtual ICollection<ItContractItSystemUsage> Contracts { get; set; }
        /// <summary>
        /// Gets or sets the organization units associated with this instance.
        /// </summary>
        /// <value>
        /// The organization units.
        /// </value>
        public virtual ICollection<OrganizationUnit> OrgUnits { get; set; }
        /// <summary>
        /// Gets or sets the organization units that are using this instance.
        /// </summary>
        /// <remarks>
        /// Must be organization units that belongs to <see cref="Organization"/>.
        /// </remarks>
        /// <value>
        /// The organization units used by this instance.
        /// </value>
        public virtual ICollection<ItSystemUsageOrgUnitUsage> UsedBy { get; set; }
        /// <summary>
        /// Gets or sets the tasks this instance supports.
        /// </summary>
        /// <value>
        /// The supported tasks.
        /// </value>
        public virtual ICollection<TaskRef> TaskRefs { get; set; }
        /// <summary>
        /// Gets or sets the tasks that has been opted out from by an organization.
        /// </summary>
        public virtual ICollection<TaskRef> TaskRefsOptOut { get; set; }
        /// <summary>
        /// Gets or sets the associated it projects.
        /// </summary>
        /// <remarks>
        /// <see cref="ItProject.ItProject"/> have a corresponding property linking back.
        /// </remarks>
        /// <value>
        /// Associated it projects.
        /// </value>
        public virtual ICollection<ItProject.ItProject> ItProjects { get; set; }

        public virtual ICollection<ExternalReference> ExternalReferences { get; set; }
        public ReferenceRootType GetRootType()
        {
            return ReferenceRootType.SystemUsage;
        }

        public Result<ExternalReference, OperationError> AddExternalReference(ExternalReference newReference)
        {
            return new AddReferenceCommand(this).AddExternalReference(newReference);
        }

        public Result<ExternalReference, OperationError> SetMasterReference(ExternalReference newReference)
        {
            Reference = newReference;
            return newReference;
        }

        public int? ReferenceId { get; set; }
        public virtual ExternalReference Reference { get; set; }
        public virtual ICollection<AccessType> AccessTypes { get; set; }

        public ArchiveDutyTypes? ArchiveDuty { get; set; }

        public bool? ReportedToDPA { get; set; }

        public string DocketNo { get; set; }

        [Column(TypeName = "date")]
        public DateTime? ArchivedDate { get; set; }

        public string ArchiveNotes { get; set; }

        public int? ArchiveFreq { get; set; }

        public string ArchiveSupplier { get; set; }

        public bool? Registertype { get; set; }

        public int? SupplierId { get; set; }
        /// <summary>
        ///     Gets or sets the organization marked as supplier for this contract.
        /// </summary>
        /// <value>
        ///     The organization.
        /// </value>
        public int? ArchiveLocationId { get; set; }

        public virtual ArchiveLocation ArchiveLocation { get; set; }

        public int? ArchiveTestLocationId { get; set; }

        public virtual ArchiveTestLocation ArchiveTestLocation { get; set; }

        public int? ItSystemCategoriesId { get; set; }

        public virtual ItSystemCategories ItSystemCategories { get; set; }




        public UserCount UserCount { get; set; }

        public string systemCategories { get; set; }


        #region GDPR
        public string GeneralPurpose { get; set; }
        public DataOptions? isBusinessCritical { get; set; }


        public string LinkToDirectoryUrl { get; set; }
        public string LinkToDirectoryUrlName { get; set; }


        public virtual ICollection<ItSystemUsageSensitiveDataLevel> SensitiveDataLevels { get; set; }

        public DataOptions? precautions { get; set; }
        public bool precautionsOptionsEncryption { get; set; }
        public bool precautionsOptionsPseudonomisering { get; set; }
        public bool precautionsOptionsAccessControl { get; set; }
        public bool precautionsOptionsLogning { get; set; }
        public string TechnicalSupervisionDocumentationUrlName { get; set; }
        public string TechnicalSupervisionDocumentationUrl { get; set; }

        public DataOptions? UserSupervision { get; set; }
        public DateTime? UserSupervisionDate { get; set; }
        public string UserSupervisionDocumentationUrlName { get; set; }
        public string UserSupervisionDocumentationUrl { get; set; }

        public DataOptions? riskAssessment { get; set; }
        public DateTime? riskAssesmentDate { get; set; }
        public RiskLevel? preriskAssessment { get; set; }
        public string RiskSupervisionDocumentationUrlName { get; set; }
        public string RiskSupervisionDocumentationUrl { get; set; }
        public string noteRisks { get; set; }

        public DataOptions? DPIA { get; set; }
        public DateTime? DPIADateFor { get; set; }
        public string DPIASupervisionDocumentationUrlName { get; set; }
        public string DPIASupervisionDocumentationUrl { get; set; }

        public DataOptions? answeringDataDPIA { get; set; }
        public DateTime? DPIAdeleteDate { get; set; }
        public int numberDPIA { get; set; }

        public HostedAt? HostedAt { get; set; }
        #endregion


        public virtual ICollection<ArchivePeriod> ArchivePeriods { get; set; }

        public bool? ArchiveFromSystem { get; set; }
        /// <summary>
        /// Defines how this system uses other systems.
        /// </summary>
        public virtual ICollection<SystemRelation> UsageRelations { get; set; }
        /// <summary>
        /// Defines how this system is used by other systems
        /// </summary>
        public virtual ICollection<SystemRelation> UsedByRelations { get; set; }
        /// <summary>
        /// DPAs using this system
        /// </summary>
        public virtual ICollection<DataProcessingRegistration> AssociatedDataProcessingRegistrations { get; set; }

        public bool HasDataProcessingAgreement() =>
            AssociatedDataProcessingRegistrations?.Any(x => x.IsAgreementConcluded == YesNoIrrelevantOption.YES) == true;


        public Result<SystemRelation, OperationError> AddUsageRelationTo(
            ItSystemUsage toSystemUsage,
            Maybe<ItInterface> relationInterface,
            string description,
            string reference,
            Maybe<RelationFrequencyType> targetFrequency,
            Maybe<ItContract.ItContract> targetContract)
        {
            if (toSystemUsage == null)
                throw new ArgumentNullException(nameof(toSystemUsage));

            var newRelation = new SystemRelation(this);

            var updateRelationResult = UpdateRelation(newRelation, toSystemUsage, description, reference, relationInterface, targetContract, targetFrequency);

            if (updateRelationResult.Failed)
            {
                return updateRelationResult.Error;
            }

            UsageRelations.Add(newRelation);

            return newRelation;
        }

        public Result<SystemRelation, OperationError> ModifyUsageRelation(
            int relationId,
            ItSystemUsage toSystemUsage,
            string changedDescription,
            string changedReference,
            Maybe<ItInterface> relationInterface,
            Maybe<ItContract.ItContract> toContract,
            Maybe<RelationFrequencyType> toFrequency)
        {
            var relationResult = GetUsageRelation(relationId);
            if (relationResult.IsNone)
            {
                return Result<SystemRelation, OperationError>.Failure(OperationFailure.BadInput);
            }

            var relation = relationResult.Value;

            return UpdateRelation(relation, toSystemUsage, changedDescription, changedReference, relationInterface, toContract, toFrequency);
        }

        public Result<SystemRelation, OperationFailure> RemoveUsageRelation(int relationId)
        {
            var relationResult = GetUsageRelation(relationId);

            if (!relationResult.HasValue)
            {
                return OperationFailure.NotFound;
            }

            var relation = relationResult.Value;
            UsageRelations.Remove(relation);
            return relation;
        }

        public IEnumerable<ItInterface> GetExposedInterfaces()
        {
            return ItSystem
                .FromNullable()
                .Select(system => system.ItInterfaceExhibits)
                .Select(interfaceExhibits => interfaceExhibits.Select(interfaceExhibit => interfaceExhibit.ItInterface))
                .Select(interfaces => interfaces.ToList())
                .GetValueOrFallback(new List<ItInterface>());
        }

        public Maybe<ItInterface> GetExposedInterface(int interfaceId)
        {
            return GetExposedInterfaces().FirstOrDefault(x => x.Id == interfaceId);
        }

        public bool HasExposedInterface(int interfaceId)
        {
            return GetExposedInterface(interfaceId).HasValue;
        }

        public Maybe<SystemRelation> GetUsageRelation(int relationId)
        {
            return UsageRelations.FirstOrDefault(r => r.Id == relationId);
        }

        private Result<SystemRelation, OperationError> UpdateRelation(
            SystemRelation relation,
            ItSystemUsage toSystemUsage,
            string changedDescription,
            string changedReference,
            Maybe<ItInterface> relationInterface,
            Maybe<ItContract.ItContract> toContract,
            Maybe<RelationFrequencyType> toFrequency)
        {
            return relation
                .SetRelationTo(toSystemUsage)
                .Select(_ => _.SetDescription(changedDescription))
                .Select(_ => _.SetRelationInterface(relationInterface))
                .Select(_ => _.SetContract(toContract))
                .Select(_ => _.SetFrequency(toFrequency))
                .Select(_ => _.SetReference(changedReference));
        }

        public Result<ItSystemUsageSensitiveDataLevel, OperationError> AddSensitiveDataLevel(
            SensitiveDataLevel sensitiveDataLevel)
        {
            if (SensitiveDataLevelExists(sensitiveDataLevel))
            {
                return new OperationError("Data sensitivity level already exists", OperationFailure.Conflict);
            }

            var newDataLevel = new ItSystemUsageSensitiveDataLevel()
            {
                ItSystemUsage = this,
                SensitivityDataLevel = sensitiveDataLevel
            };

            SensitiveDataLevels.Add(newDataLevel);

            return newDataLevel;
        }

        public Result<ItSystemUsageSensitiveDataLevel, OperationError> RemoveSensitiveDataLevel(
            SensitiveDataLevel sensitiveDataLevel)
        {
            if (!SensitiveDataLevelExists(sensitiveDataLevel))
            {
                return new OperationError("Data sensitivity does not exists on system usage", OperationFailure.NotFound);
            }

            var dataLevelToRemove = SensitiveDataLevels.First(x => x.SensitivityDataLevel == sensitiveDataLevel);
            SensitiveDataLevels.Remove(dataLevelToRemove);

            return dataLevelToRemove;
        }

        private bool SensitiveDataLevelExists(SensitiveDataLevel sensitiveDataLevel)
        {
            return SensitiveDataLevels.Any(x => x.SensitivityDataLevel == sensitiveDataLevel);
        }
    }
}
