﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.DomainModel.GDPR.Read;
using Core.DomainModel.ItContract;
using Core.DomainModel.References;
using Core.DomainModel.Result;
using Core.DomainModel.Shared;
using Infrastructure.Services.Types;

namespace Core.DomainModel.GDPR
{
    public class DataProcessingRegistration :
        HasRightsEntity<DataProcessingRegistration, DataProcessingRegistrationRight, DataProcessingRegistrationRole>,
        IHasName,
        IOwnedByOrganization,
        IDataProcessingModule,
        IEntityWithExternalReferences

    {
        public DataProcessingRegistration()
        {
            ExternalReferences = new List<ExternalReference>();
            SystemUsages = new List<ItSystemUsage.ItSystemUsage>();
            DataProcessors = new List<Organization.Organization>();
            SubDataProcessors = new List<Organization.Organization>();
            InsecureCountriesSubjectToDataTransfer = new List<DataProcessingCountryOption>();
            OversightOptions = new List<DataProcessingOversightOption>();
            AssociatedContracts = new List<ItContract.ItContract>();
        }

        public static bool IsNameValid(string name) => !string.IsNullOrWhiteSpace(name) &&
                                                       name.Length <= DataProcessingRegistrationConstraints.MaxNameLength;

        public Maybe<OperationError> SetName(string newName)
        {
            if (IsNameValid(newName))
            {
                Name = newName;
                return Maybe<OperationError>.None;
            }
            return new OperationError("Name does not meet validation criteria", OperationFailure.BadInput);
        }

        public string Name { get; set; }

        public int OrganizationId { get; set; }

        public YesNoUndecidedOption? HasSubDataProcessors { get; set; }

        public void SetHasSubDataProcessors(YesNoUndecidedOption hasSubDataProcessors)
        {
            HasSubDataProcessors = hasSubDataProcessors;
            if (hasSubDataProcessors != YesNoUndecidedOption.Yes)
            {
                SubDataProcessors.Clear();
            }
        }

        public YesNoUndecidedOption? TransferToInsecureThirdCountries { get; set; }

        public void SetTransferToInsecureThirdCountries(YesNoUndecidedOption transferToInsecureThirdCountries)
        {
            TransferToInsecureThirdCountries = transferToInsecureThirdCountries;
            if (transferToInsecureThirdCountries != YesNoUndecidedOption.Yes)
            {
                InsecureCountriesSubjectToDataTransfer.Clear();
            }
        }

        public virtual ICollection<DataProcessingCountryOption> InsecureCountriesSubjectToDataTransfer { get; set; }

        public virtual Organization.Organization Organization { get; set; }

        public virtual ICollection<DataProcessingRegistrationReadModel> ReadModels { get; set; }

        public virtual ICollection<ItSystemUsage.ItSystemUsage> SystemUsages { get; set; }

        public virtual ICollection<Organization.Organization> DataProcessors { get; set; }

        public virtual ICollection<Organization.Organization> SubDataProcessors { get; set; }

        public virtual DataProcessingDataResponsibleOption DataResponsible { get; set; }
        public int? DataResponsible_Id { get; set; }

        public string DataResponsibleRemark { get; set; }

        public virtual ICollection<DataProcessingOversightOption> OversightOptions { get; set; }
        public string OversightOptionRemark { get; set; }

        public IEnumerable<DataProcessingRegistrationRight> GetRights(int roleId)
        {
            return Rights.Where(x => x.RoleId == roleId);
        }

        public Result<Organization.Organization, OperationError> AssignDataProcessor(Organization.Organization dataProcessor)
        {
            if (dataProcessor == null) throw new ArgumentNullException(nameof(dataProcessor));
            if (HasDataProcessor(dataProcessor))
                return new OperationError("Data processor already assigned", OperationFailure.Conflict);

            DataProcessors.Add(dataProcessor);

            return dataProcessor;
        }

        public Result<Organization.Organization, OperationError> RemoveDataProcessor(Organization.Organization dataProcessor)
        {
            if (dataProcessor == null) throw new ArgumentNullException(nameof(dataProcessor));
            if (!HasDataProcessor(dataProcessor))
                return new OperationError("Data processor not assigned", OperationFailure.BadInput);

            DataProcessors.Remove(dataProcessor);

            return dataProcessor;
        }

        public Result<Organization.Organization, OperationError> AssignSubDataProcessor(Organization.Organization dataProcessor)
        {
            if (dataProcessor == null) throw new ArgumentNullException(nameof(dataProcessor));

            if (HasSubDataProcessors != YesNoUndecidedOption.Yes)
                return new OperationError("To Add new sub data processors, enable sub data processors", OperationFailure.BadInput);

            if (HasSubDataProcessor(dataProcessor))
                return new OperationError("Sub Data processor already assigned", OperationFailure.Conflict);

            SubDataProcessors.Add(dataProcessor);

            return dataProcessor;
        }

        public Result<Organization.Organization, OperationError> RemoveSubDataProcessor(Organization.Organization dataProcessor)
        {
            if (dataProcessor == null) throw new ArgumentNullException(nameof(dataProcessor));
            if (!HasSubDataProcessor(dataProcessor))
                return new OperationError("Sub Data processor not assigned", OperationFailure.BadInput);

            SubDataProcessors.Remove(dataProcessor);

            return dataProcessor;
        }

        public Result<DataProcessingCountryOption, OperationError> AssignInsecureCountrySubjectToDataTransfer(DataProcessingCountryOption country)
        {
            if (country == null) throw new ArgumentNullException(nameof(country));

            if (TransferToInsecureThirdCountries != YesNoUndecidedOption.Yes)
                return new OperationError("To Add new insecure data transfer country, but transfer to insecure countries is not enabled", OperationFailure.BadInput);

            if (HasInsecureCountry(country))
                return new OperationError("Country already assigned", OperationFailure.Conflict);

            InsecureCountriesSubjectToDataTransfer.Add(country);

            return country;
        }

        public Result<DataProcessingCountryOption, OperationError> RemoveInsecureCountrySubjectToDataTransfer(DataProcessingCountryOption country)
        {
            if (country == null) throw new ArgumentNullException(nameof(country));

            if (!HasInsecureCountry(country))
                return new OperationError("Country not assigned", OperationFailure.Conflict);

            InsecureCountriesSubjectToDataTransfer.Remove(country);

            return country;
        }

        public Result<DataProcessingOversightOption, OperationError> AssignOversightOption(DataProcessingOversightOption oversightOption)
        {
            if (oversightOption == null) throw new ArgumentNullException(nameof(oversightOption));
            if (HasOversightOption(oversightOption))
                return new OperationError("Oversight option already assigned", OperationFailure.Conflict);

            OversightOptions.Add(oversightOption);

            return oversightOption;
        }

        public Result<DataProcessingOversightOption, OperationError> RemoveOversightOption(DataProcessingOversightOption oversightOption)
        {
            if (oversightOption == null) throw new ArgumentNullException(nameof(oversightOption));
            if (!HasOversightOption(oversightOption))
                return new OperationError("Oversight option not assigned", OperationFailure.BadInput);

            OversightOptions.Remove(oversightOption);

            return oversightOption;
        }

        private bool HasInsecureCountry(DataProcessingCountryOption country)
        {
            return InsecureCountriesSubjectToDataTransfer.Any(c => c.Id == country.Id);
        }

        private bool HasSubDataProcessor(Organization.Organization dataProcessor)
        {
            return SubDataProcessors.Any(x => x.Id == dataProcessor.Id);
        }

        private bool HasDataProcessor(Organization.Organization dataProcessor)
        {
            return DataProcessors.Any(x => x.Id == dataProcessor.Id);
        }

        private bool HasOversightOption(DataProcessingOversightOption oversightOption)
        {
            return OversightOptions.Any(x => x.Id == oversightOption.Id);
        }

        public Result<ItSystemUsage.ItSystemUsage, OperationError> AssignSystem(ItSystemUsage.ItSystemUsage systemUsage)
        {
            if (systemUsage == null) throw new ArgumentNullException(nameof(systemUsage));

            if (GetAssignedSystemUsage(systemUsage.Id).HasValue)
                return new OperationError("System usage is already assigned", OperationFailure.Conflict);

            SystemUsages.Add(systemUsage);

            return systemUsage;
        }

        public Result<ItSystemUsage.ItSystemUsage, OperationError> RemoveSystem(ItSystemUsage.ItSystemUsage systemUsage)
        {
            if (systemUsage == null) throw new ArgumentNullException(nameof(systemUsage));

            if (GetAssignedSystemUsage(systemUsage.Id).IsNone)
                return new OperationError("Usage not assigned", OperationFailure.BadInput);

            SystemUsages.Remove(systemUsage);

            return systemUsage;
        }

        private Maybe<ItSystemUsage.ItSystemUsage> GetAssignedSystemUsage(int usageId)
        {
            return SystemUsages.FirstOrDefault(x => x.Id == usageId).FromNullable();
        }

        public Result<DataProcessingRegistrationRight, OperationError> AssignRoleToUser(DataProcessingRegistrationRole role, User user)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            if (user == null) throw new ArgumentNullException(nameof(user));

            if (HasRight(role, user))
                return new OperationError("Existing right for same role found for the same user", OperationFailure.Conflict);

            var newRight = new DataProcessingRegistrationRight
            {
                Role = role,
                User = user,
                Object = this
            };

            Rights.Add(newRight);

            return newRight;
        }

        private bool HasRight(DataProcessingRegistrationRole role, User user)
        {
            return GetRight(role, user).HasValue;
        }

        private Maybe<DataProcessingRegistrationRight> GetRight(DataProcessingRegistrationRole role, User user)
        {
            return GetRights(role.Id).FirstOrDefault(x => x.UserId == user.Id);
        }

        public Result<DataProcessingRegistrationRight, OperationError> RemoveRole(DataProcessingRegistrationRole role, User user)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            if (user == null) throw new ArgumentNullException(nameof(user));

            return GetRight(role, user)
                .Match<Result<DataProcessingRegistrationRight, OperationError>>
                (
                    right =>
                    {
                        Rights.Remove(right);
                        return right;
                    },
                    () => new OperationError($"Role with id {role.Id} is not assigned to user with id ${user.Id}",
                        OperationFailure.BadInput)
                );

        }

        public virtual ICollection<ExternalReference> ExternalReferences { get; set; }

        public ReferenceRootType GetRootType() => ReferenceRootType.DataProcessingRegistration;

        public Result<ExternalReference, OperationError> AddExternalReference(ExternalReference newReference)
        {
            return new AddReferenceCommand(this).AddExternalReference(newReference);
        }

        public Result<ExternalReference, OperationError> SetMasterReference(ExternalReference newReference)
        {
            if (ExternalReferences.Any(x => x.Id == newReference.Id))
            {
                Reference = newReference;
                return newReference;
            }
            return new OperationError("Reference is not known to this object", OperationFailure.BadInput);
        }

        public int? ReferenceId { get; set; }

        public virtual ExternalReference Reference { get; set; }

        public YesNoIrrelevantOption? IsAgreementConcluded { get; set; }

        public DateTime? AgreementConcludedAt { get; set; }

        public string AgreementConcludedRemark { get; set; }

        public int? BasisForTransferId { get; set; }

        public virtual DataProcessingBasisForTransferOption BasisForTransfer { get; set; }

        public YearMonthIntervalOption? OversightInterval { get; set; }

        public string OversightIntervalRemark { get; set; }

        public void SetIsAgreementConcluded(YesNoIrrelevantOption concluded)
        {
            IsAgreementConcluded = concluded;
            if (IsAgreementConcluded != YesNoIrrelevantOption.YES)
            {
                AgreementConcludedAt = null;
            }
        }

        public YesNoUndecidedOption? IsOversightCompleted { get; set; }

        public DateTime? LatestOversightDate { get; set; }

        public string OversightCompletedRemark { get; set; }

        public void SetOversightCompleted(YesNoUndecidedOption completed)
        {
            IsOversightCompleted = completed;
            if (IsOversightCompleted != YesNoUndecidedOption.Yes)
            {
                LatestOversightDate = null;
            }

        }

        public virtual ICollection<ItContract.ItContract> AssociatedContracts { get; set; }
    }
}
