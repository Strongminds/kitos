﻿using System;
using System.Collections.Generic;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.GDPR;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.ApplicationServices.Model.Shared;
using Core.ApplicationServices.Model.Shared.Write;
using Core.DomainModel;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Shared;
using Moq;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Presentation.Web.Authorization
{
    public class SupplierAssociatedFieldsServiceTest: WithAutoFixture
    {
        private readonly SupplierAssociatedFieldsService _sut;
        private readonly Mock<IDataProcessingRegistrationApplicationService> _dataProcessingRegistrationApplicationService;
        private readonly int _dprId;
        private readonly DataProcessingRegistration _existingDpr;

        public SupplierAssociatedFieldsServiceTest()
        {
            _dataProcessingRegistrationApplicationService =
                new Mock<IDataProcessingRegistrationApplicationService>();
            _dprId = A<int>();
            _existingDpr = new DataProcessingRegistration() { Id = _dprId };
            _dataProcessingRegistrationApplicationService.Setup(_ => _.Get(_dprId))
                .Returns(Result<DataProcessingRegistration, OperationError>.Success(_existingDpr));

            _sut = new SupplierAssociatedFieldsService(_dataProcessingRegistrationApplicationService.Object);
        }
        
        [Theory]
        [InlineData(true, false, false, false)]
        public void DprParams_GivenChangesToAnySupplierAssociatedField_RequestsChangesToSupplierAssociatedFields_ReturnsTrue(bool checkIsOversightCompleted, bool checkOversightDate, bool checkOversightNotes, bool checkOversightReportLink)
        {
            var oversight = new UpdatedDataProcessingRegistrationOversightDataParameters();
            
            if (checkIsOversightCompleted)
                oversight.IsOversightCompleted = A<YesNoUndecidedOption?>().AsChangedValue();
    
            var parameters = new DataProcessingRegistrationModificationParameters()
            {
                Oversight = oversight
            };
            var result = _sut.RequestsChangesToSupplierAssociatedFields(parameters);

            Assert.True(result);
        }

        [Theory]
        [InlineData(true, false, false, false)]
        public void DprParams_GivenChangesToAnySupplierAssociatedField_RequestsChangesToNonSupplierAssociatedFields_ReturnsFalse(bool checkIsOversightCompleted, bool checkOversightDate, bool checkOversightNotes, bool checkOversightReportLink)
        {
            var oversight = new UpdatedDataProcessingRegistrationOversightDataParameters();

            if (checkIsOversightCompleted)
                oversight.IsOversightCompleted = A<YesNoUndecidedOption?>().AsChangedValue();
            //TODO same as above, awaiting response from MIOL about what fields to target

            var parameters = new DataProcessingRegistrationModificationParameters()
            {
                Oversight = oversight
            };
            var result = _sut.RequestsChangesToNonSupplierAssociatedFields(parameters, _dprId);

            Assert.False(result);
        }

        [Theory]
        [InlineData(true, false, false, false, false, false)]
        [InlineData(false, true, false, false, false, false)]
        [InlineData(false, false, true, false, false, false)]
        [InlineData(false, false, false, true, false, false)]
        [InlineData(false, false, false, false, true, false)]
        [InlineData(false, false, false, false, false, true)] 
        public void DprParams_GivenChangesToANonSupplierAssociatedField_RequestsChangesToNonSupplierAssociatedFields_ReturnsTrue(bool addChangeToName, bool addChangeToGeneral,
            bool addChangeToSystemUsageUuids, bool addNonSupplierChangeToOversight, bool addChangeToRoles, bool addChangeToExternalReferences)
        {
            var parameters = new DataProcessingRegistrationModificationParameters();
            if (addChangeToName)
            {
                parameters.Name = A<string>().AsChangedValue();
            }
            if (addChangeToGeneral)
            {
                parameters.General = Maybe<UpdatedDataProcessingRegistrationGeneralDataParameters>.Some(new UpdatedDataProcessingRegistrationGeneralDataParameters());
                parameters.General.Value.AgreementConcludedAt = A<DateTime?>().AsChangedValue();
            }
            if (addChangeToSystemUsageUuids)
            {
                var existingSystemUsages = new List<ItSystemUsage>(){ new(){ Uuid = A<Guid>() }, new(){Uuid = A<Guid>() } };
                _existingDpr.SystemUsages = existingSystemUsages;
                parameters.SystemUsageUuids = Maybe<IEnumerable<Guid>>.Some(Many<Guid>());
            }

            if (addNonSupplierChangeToOversight)
            {
                parameters.Oversight = Maybe<UpdatedDataProcessingRegistrationOversightDataParameters>.Some(new UpdatedDataProcessingRegistrationOversightDataParameters());
                parameters.Oversight.Value.OversightCompletedRemark = A<string>().AsChangedValue();
            }
            if (addChangeToRoles)
            {
                parameters.Roles = Maybe<UpdatedDataProcessingRegistrationRoles>.Some(new UpdatedDataProcessingRegistrationRoles());
                parameters.Roles.Value.UserRolePairs =
                    Maybe<IEnumerable<UserRolePair>>.Some(new List<UserRolePair>()).AsChangedValue();
            }
            if (addChangeToExternalReferences)
            {
                var existingReferences = new List<ExternalReference>(){new ExternalReference(), new ExternalReference()};
                _existingDpr.ExternalReferences = existingReferences;
                parameters.ExternalReferences = Maybe<IEnumerable<UpdatedExternalReferenceProperties>>.Some(Many<UpdatedExternalReferenceProperties>());
            }

            var result = _sut.RequestsChangesToNonSupplierAssociatedFields(parameters, _dprId);

            Assert.True(result);
        }

        [Theory]
        [InlineData(true, false, false, false, false, false)]
        [InlineData(false, true, false, false, false, false)]
        [InlineData(false, false, true, false, false, false)]
        [InlineData(false, false, false, true, false, false)]
        [InlineData(false, false, false, false, true, false)]
        [InlineData(false, false, false, false, false, true)]
        public void DprParams_GivenChangesToANonSupplierAssociatedField_RequestsChangesToSupplierAssociatedFields_ReturnsFalse(bool addChangeToName, bool addChangeToGeneral,
            bool addChangeToSystemUsageUuids, bool addNonSupplierChangeToOversight, bool addChangeToRoles, bool addChangeToExternalReferences)
        {
            var parameters = new DataProcessingRegistrationModificationParameters();
            if (addChangeToName)
            {
                parameters.Name = A<string>().AsChangedValue();
            }
            if (addChangeToGeneral)
            {
                parameters.General = Maybe<UpdatedDataProcessingRegistrationGeneralDataParameters>.Some(new UpdatedDataProcessingRegistrationGeneralDataParameters());
                parameters.General.Value.AgreementConcludedAt = A<DateTime?>().AsChangedValue();
            }
            if (addChangeToSystemUsageUuids)
            {
                var existingSystemUsages = new List<ItSystemUsage>() { new() { Uuid = A<Guid>() }, new() { Uuid = A<Guid>() } };
                _existingDpr.SystemUsages = existingSystemUsages;
                parameters.SystemUsageUuids = Maybe<IEnumerable<Guid>>.Some(Many<Guid>());
            }

            if (addNonSupplierChangeToOversight)
            {
                parameters.Oversight = Maybe<UpdatedDataProcessingRegistrationOversightDataParameters>.Some(new UpdatedDataProcessingRegistrationOversightDataParameters());
                parameters.Oversight.Value.OversightCompletedRemark = A<string>().AsChangedValue();
            }
            if (addChangeToRoles)
            {
                parameters.Roles = Maybe<UpdatedDataProcessingRegistrationRoles>.Some(new UpdatedDataProcessingRegistrationRoles());
                parameters.Roles.Value.UserRolePairs =
                    Maybe<IEnumerable<UserRolePair>>.Some(new List<UserRolePair>()).AsChangedValue();
            }
            if (addChangeToExternalReferences)
            {
                var existingReferences = new List<ExternalReference>() { new ExternalReference(), new ExternalReference() };
                _existingDpr.ExternalReferences = existingReferences;
                parameters.ExternalReferences = Maybe<IEnumerable<UpdatedExternalReferenceProperties>>.Some(Many<UpdatedExternalReferenceProperties>());
            }

            var result = _sut.RequestsChangesToSupplierAssociatedFields(parameters);

            Assert.False(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void
            GivenNoChanges_RequestsChangesToSupplierAssociatedFields_And_RequestsChangesToNonSupplierAssociatedFields_BothReturnFalse(bool dprParams)
        {
            bool requestsChangesToSupplierAssociatedFields;
            bool requestsChangesToNonSupplierAssociatedFields;
            if (dprParams)
            {
                var noChangesParameters = new DataProcessingRegistrationModificationParameters();
                requestsChangesToSupplierAssociatedFields =
                    _sut.RequestsChangesToSupplierAssociatedFields(noChangesParameters);
                requestsChangesToNonSupplierAssociatedFields = _sut.RequestsChangesToNonSupplierAssociatedFields(noChangesParameters, _dprId);
            }
            else
            {
                var noChangesParameters = new UpdatedDataProcessingRegistrationOversightDateParameters
                    {
                        CompletedAt = OptionalValueChange<DateTime>.None,
                        Remark = OptionalValueChange<string>.None,
                        OversightReportLink = OptionalValueChange<string>.None
                    };
                requestsChangesToSupplierAssociatedFields =
                    _sut.RequestsChangesToSupplierAssociatedFields(noChangesParameters);
                requestsChangesToNonSupplierAssociatedFields = _sut.RequestsChangesToNonSupplierAssociatedFields(noChangesParameters, _dprId);
            }

            Assert.False(requestsChangesToSupplierAssociatedFields);
            Assert.False(requestsChangesToNonSupplierAssociatedFields);
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void
            DprOversightDateParams_GivenChangesToSupplierFields_RequestsChangesToSupplierAssociatedFields_Returns_True(bool completedAt, bool remark, bool oversightReportLink)
        {
            var parameters = new UpdatedDataProcessingRegistrationOversightDateParameters
            {
                CompletedAt = OptionalValueChange<DateTime>.None,
                Remark = OptionalValueChange<string>.None,
                OversightReportLink = OptionalValueChange<string>.None
            };
            if (completedAt)
                parameters.CompletedAt = A<DateTime>().AsChangedValue();
            if (remark)
                parameters.Remark = A<string>().AsChangedValue();
            if (oversightReportLink)
                parameters.OversightReportLink = A<string>().AsChangedValue();

            var result = _sut.RequestsChangesToSupplierAssociatedFields(parameters);

            Assert.True(result);
        }
    }
}
