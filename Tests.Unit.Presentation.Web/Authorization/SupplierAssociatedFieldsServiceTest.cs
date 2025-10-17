using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.ApplicationServices.Model.Shared;
using Core.ApplicationServices.Model.Shared.Write;
using Core.DomainModel;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.Shared;
using Core.DomainServices.Suppliers;
using Moq;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Presentation.Web.Authorization
{
    public class SupplierAssociatedFieldsServiceTest: WithAutoFixture
    {
        private readonly SupplierAssociatedFieldsService _sut;
        private readonly int _dprId;
        private readonly DataProcessingRegistration _existingDpr;
        private readonly Mock<ISupplierFieldDomainService> _supplierDomainServiceMock;
        public SupplierAssociatedFieldsServiceTest()
        {
           _dprId = A<int>();
           _existingDpr = new DataProcessingRegistration() { Id = _dprId };
           _supplierDomainServiceMock = new Mock<ISupplierFieldDomainService>();
           _sut = new SupplierAssociatedFieldsService(_supplierDomainServiceMock.Object);
        }

        [Fact]
        public void HasAnySupplierChanges_Returns_True_If_Dpr_Has_Supplier_Changes()
        {
            //Arrange
            var oversight = new UpdatedDataProcessingRegistrationOversightDataParameters
            {
                IsOversightCompleted = A<YesNoUndecidedOption?>().AsChangedValue()
            };

            var parameters = new DataProcessingRegistrationModificationParameters()
            {
                Oversight = oversight
            };

            var keys = _sut.MapParameterKeysToDomainKeys(parameters.GetChangedPropertyKeys(_existingDpr)).ToList();

            ExpectOnlySupplierChangesReturns(keys, true);

            //Act
            var result = _sut.HasAnySupplierChanges(parameters, _existingDpr);

            //Assert
            Assert.True(result);
        }

        [Fact]
        public void HasOnlySupplierChanges_Returns_False_If_OversightDate_Has_Non_Supplier_Changes()
        {
            var oversight = new UpdatedDataProcessingRegistrationOversightDataParameters();
            oversight.IsOversightCompleted = A<YesNoUndecidedOption?>().AsChangedValue();

            var parameters = new DataProcessingRegistrationModificationParameters()
            {
                Oversight = oversight
            };


            var keys = _sut.MapParameterKeysToDomainKeys(parameters.GetChangedPropertyKeys(_existingDpr));

            ExpectOnlySupplierChangesReturns(keys, false);
            var result = _sut.HasOnlySupplierChanges(parameters, _existingDpr);

            Assert.False(result);
        }

        [Theory]
        [InlineData(true, false, false, false, false, false)]
        [InlineData(false, true, false, false, false, false)]
        [InlineData(false, false, true, false, false, false)]
        [InlineData(false, false, false, true, false, false)]
        [InlineData(false, false, false, false, true, false)]
        [InlineData(false, false, false, false, false, true)] 
        public void HasOnlySupplierChanges_Returns_False_If_Dpr_Has_Non_Supplier_Changes(bool addChangeToName, bool addChangeToGeneral,
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

            var keys = parameters.GetChangedPropertyKeys(_existingDpr);
            ExpectOnlySupplierChangesReturns(keys, false);

            var result = _sut.HasOnlySupplierChanges(parameters, _existingDpr);

            Assert.False(result);
        }

        [Theory]
        [InlineData(true, false, false, false, false, false)]
        [InlineData(false, true, false, false, false, false)]
        [InlineData(false, false, true, false, false, false)]
        [InlineData(false, false, false, true, false, false)]
        [InlineData(false, false, false, false, true, false)]
        [InlineData(false, false, false, false, false, true)]
        public void HasAnySupplierChanges_Returns_True_If_Has_Any_Supplier_Changes(bool addChangeToName, bool addChangeToGeneral,
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

            var keys = parameters.GetChangedPropertyKeys(_existingDpr);
            ExpectAnySupplierChangesReturns(keys, true);

            var result = _sut.HasAnySupplierChanges(parameters, _existingDpr);

            Assert.True(result);
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
                    _sut.HasAnySupplierChanges(noChangesParameters, _existingDpr);
                requestsChangesToNonSupplierAssociatedFields = _sut.HasOnlySupplierChanges(noChangesParameters, _existingDpr);
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
                    _sut.HasAnySupplierChanges(noChangesParameters, _existingDpr);
                requestsChangesToNonSupplierAssociatedFields = _sut.HasOnlySupplierChanges(noChangesParameters, _existingDpr);
            }

            Assert.False(requestsChangesToSupplierAssociatedFields);
            Assert.False(requestsChangesToNonSupplierAssociatedFields);
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void DprOversightDateParams_GivenChangesToSupplierFields_RequestsChangesToSupplierAssociatedFields_Returns_True(bool completedAt, bool remark, bool oversightReportLink)
        {
            var parameters = GetOversightDateParametersWithChange(completedAt, remark, oversightReportLink);

            var keys = parameters.GetChangedPropertyKeys();
            ExpectOnlySupplierChangesReturns(keys, true);

            var result = _sut.HasAnySupplierChanges(parameters, _existingDpr);
            Assert.True(result);
        }

        [Fact]
        public void GivenNoChanges_RequestsChangesToSupplierAssociatedFieldsInEnumerable_Returns_False()
        {
            var parametersEnumerable = new List<UpdatedDataProcessingRegistrationOversightDateParameters>()
            {
                new()
                {
                    CompletedAt = OptionalValueChange<DateTime>.None,
                    OversightReportLink = OptionalValueChange<string>.None,
                    Remark = OptionalValueChange<string>.None
                }
            };

            var requestsChangesToSupplierAssociatedFields = _sut.RequestsChangesToSupplierAssociatedFieldsInEnumerable(parametersEnumerable, _existingDpr);

            Assert.False(requestsChangesToSupplierAssociatedFields);
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void RequestsChangesToSupplierAssociatedFieldsInEnumerable_Returns_True(
            bool completedAt, bool remark, bool oversightReportLink)
        {
            var parameters = GetOversightDateParametersWithChange(completedAt, remark, oversightReportLink);
            var parametersEnumerable = new List<UpdatedDataProcessingRegistrationOversightDateParameters>()
            {
                parameters
            };

            var keys = parameters.GetChangedPropertyKeys();
            ExpectOnlySupplierChangesReturns(keys, true);
            var result = _sut.RequestsChangesToSupplierAssociatedFieldsInEnumerable(parametersEnumerable, _existingDpr);

            Assert.True(result);
        }

        private void ExpectOnlySupplierChangesReturns(IEnumerable<string> keys, bool result)
        {
            _supplierDomainServiceMock
                .Setup(x => x.OnlySupplierFieldChanges(
                    It.Is<IEnumerable<string>>(actualKeys =>
                        actualKeys.All(keys.Contains)
                    )
                ))
                .Returns(result);

        }

        private void ExpectAnySupplierChangesReturns(IEnumerable<string> keys, bool result)
        {
            _supplierDomainServiceMock.Setup(x => x.AnySupplierFieldChanges(It.Is<IEnumerable<string>>(actualKeys =>
                    actualKeys.All(keys.Contains))))
                .Returns(result);
        }

        private UpdatedDataProcessingRegistrationOversightDateParameters GetOversightDateParametersWithChange(bool completedAt, bool remark, bool oversightReportLink)
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

            return parameters;
        }
    }
}
