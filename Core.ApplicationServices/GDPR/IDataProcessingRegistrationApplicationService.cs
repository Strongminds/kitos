﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Model.GDPR;
using Core.ApplicationServices.Model.GDPR.Write.SubDataProcessor;
using Core.DomainModel;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Organization;
using Core.DomainModel.Shared;
using Core.DomainServices.Queries;


namespace Core.ApplicationServices.GDPR
{
    public interface IDataProcessingRegistrationApplicationService
    {
        Result<DataProcessingRegistration, OperationError> Create(int organizationId, string name);
        Maybe<OperationError> ValidateSuggestedNewRegistrationName(int organizationId, string name);
        Result<DataProcessingRegistration, OperationError> Delete(int id);
        Result<DataProcessingRegistration, OperationError> Get(int id);
        Result<IQueryable<DataProcessingRegistration>, OperationError> GetOrganizationData(int organizationId, int skip, int take);
        Result<DataProcessingRegistration, OperationError> UpdateName(int id, string name);
        Result<ExternalReference, OperationError> SetMasterReference(int id, int referenceId);
        Result<Maybe<ExternalReference>, OperationError> ClearMasterReference(int id);
        Result<(DataProcessingRegistration registration, IEnumerable<DataProcessingRegistrationRole> roles), OperationError> GetAvailableRoles(int id);
        Result<IEnumerable<User>, OperationError> GetUsersWhichCanBeAssignedToRole(int id, int roleId, string nameEmailQuery, int pageSize);
        Result<DataProcessingRegistrationRight, OperationError> AssignRole(int id, int roleId, int userId);
        Result<DataProcessingRegistrationRight, OperationError> RemoveRole(int id, int roleId, int userId);
        Result<IEnumerable<ItSystemUsage>, OperationError> GetSystemsWhichCanBeAssigned(int id, string nameQuery, int pageSize);
        Result<ItSystemUsage, OperationError> AssignSystem(int id, int systemId);
        Result<ItSystemUsage, OperationError> RemoveSystem(int id, int systemId);
        Result<DataProcessingRegistration, OperationError> UpdateOversightInterval(int id, YearMonthIntervalOption oversightInterval);
        Result<DataProcessingRegistration, OperationError> UpdateOversightIntervalRemark(int id, string remark);
        Result<IEnumerable<Organization>, OperationError> GetDataProcessorsWhichCanBeAssigned(int id, string nameQuery, int pageSize);
        Result<Organization, OperationError> AssignDataProcessor(int id, int organizationId);
        Result<Organization, OperationError> RemoveDataProcessor(int id, int organizationId);
        Result<IEnumerable<Organization>, OperationError> GetSubDataProcessorsWhichCanBeAssigned(int id, string nameQuery, int pageSize);
        Result<DataProcessingRegistration, OperationError> SetSubDataProcessorsState(int id, YesNoUndecidedOption state);
        Result<SubDataProcessor, OperationError> AssignSubDataProcessor(int id, int organizationId, Maybe<SubDataProcessorDetailsParameters> details);
        Result<SubDataProcessor, OperationError> UpdateSubDataProcessor(int id, int organizationId, SubDataProcessorDetailsParameters details);
        Result<SubDataProcessor, OperationError> RemoveSubDataProcessor(int id, int organizationId);
        Result<DataProcessingRegistration, OperationError> UpdateIsAgreementConcluded(int id, YesNoIrrelevantOption concluded);
        Result<DataProcessingRegistration, OperationError> UpdateAgreementConcludedRemark(int id, string remark);
        Result<DataProcessingRegistration, OperationError> UpdateAgreementConcludedAt(int id, DateTime? concludedAtDate);
        Result<DataProcessingRegistration, OperationError> UpdateTransferToInsecureThirdCountries(int id, YesNoUndecidedOption transferToInsecureThirdCountries);
        Result<DataProcessingCountryOption, OperationError> AssignInsecureThirdCountry(int id, int countryId);
        Result<DataProcessingCountryOption, OperationError> RemoveInsecureThirdCountry(int id, int countryId);
        Result<DataProcessingBasisForTransferOption, OperationError> AssignBasisForTransfer(int id, int basisForTransferId);
        /// <returns>Error if clear operation fails or BadState of no basis for transfer is assigned</returns>
        Result<DataProcessingBasisForTransferOption, OperationError> ClearBasisForTransfer(int id);
        Result<DataProcessingDataResponsibleOption, OperationError> AssignDataResponsible(int id, int dataResponsibleId);
        /// <returns>Error if clear operation fails or BadState of no data responsible is assigned</returns>
        Result<DataProcessingDataResponsibleOption, OperationError> ClearDataResponsible(int id);
        Result<DataProcessingRegistration, OperationError> UpdateDataResponsibleRemark(int id, string remark);
        Result<DataProcessingOversightOption, OperationError> AssignOversightOption(int id, int oversightOptionId);
        Result<DataProcessingOversightOption, OperationError> RemoveOversightOption(int id, int oversightOptionId);
        Result<DataProcessingRegistration, OperationError> UpdateOversightOptionRemark(int id, string remark);
        Result<DataProcessingRegistration, OperationError> UpdateIsOversightCompleted(int id, YesNoUndecidedOption completed);
        Result<DataProcessingRegistration, OperationError> UpdateOversightScheduledInspectionDate(int id, DateTime? oversightScheduledInspectionDate);
        Result<DataProcessingRegistration, OperationError> UpdateOversightCompletedRemark(int id, string remark);
        Result<DataProcessingRegistrationOversightDate, OperationError> AssignOversightDate(int id, DateTime oversightDate, string oversightRemark);
        Result<DataProcessingRegistrationOversightDate, OperationError> ModifyOversightDate(int id, int oversightDateId, DateTime oversightDate, string oversightRemark);
        Result<DataProcessingRegistrationOversightDate, OperationError> RemoveOversightDate(int id, int oversightDateId);
        Result<DataProcessingRegistration, OperationError> UpdateMainContract(int id, int contractId);
        Result<DataProcessingRegistration, OperationError> RemoveMainContract(int id);

        IQueryable<DataProcessingRegistration> Query(params IDomainQuery<DataProcessingRegistration>[] conditions);
        Result<DataProcessingRegistration, OperationError> GetByUuid(Guid uuid);

        Result<DataProcessingRegistrationPermissions, OperationError> GetPermissions(Guid uuid);
        Result<ResourceCollectionPermissionsResult, OperationError> GetCollectionPermissions(Guid organizationUuid);
    }
}
