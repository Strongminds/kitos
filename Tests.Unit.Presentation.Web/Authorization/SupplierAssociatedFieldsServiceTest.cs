using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Extensions;
using Core.ApplicationServices.GDPR;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.ApplicationServices.Model.Shared.Write;
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

        public SupplierAssociatedFieldsServiceTest()
        {
            _dataProcessingRegistrationApplicationService =
                new Mock<IDataProcessingRegistrationApplicationService>();
            
            _sut = new SupplierAssociatedFieldsService(_dataProcessingRegistrationApplicationService.Object);
        }
        
        [Theory]
        [InlineData(true, false, false, false)]
        public void GivenChangesToAnySupplierAssociatedField_RequestsChangesToSupplierAssociatedFields_ReturnsTrue(bool checkIsOversightCompleted, bool checkOversightDate, bool checkOversightNotes, bool checkOversightReportLink)
        {
            var oversight = new UpdatedDataProcessingRegistrationOversightDataParameters();
            
            if (checkIsOversightCompleted)
                oversight.IsOversightCompleted = A<YesNoUndecidedOption?>().AsChangedValue();
            //TODO awaiting response from MIOL about what fields to target
            //if (checkOversightDate)
            //    parameters.Oversight.Value. = A<DateTime?>().AsChangedValue();
            
            var parameters = new DataProcessingRegistrationModificationParameters()
            {
                Oversight = oversight
            };
            var result = _sut.RequestsChangesToSupplierAssociatedFields(parameters);

            Assert.True(result);
        }

        [Theory]
        [InlineData(true, false, false, false, false, false)]
        [InlineData(false, true, false, false, false, false)]
        [InlineData(false, false, true, false, false, false)]
        [InlineData(false, false, false, true, false, false)]
        [InlineData(false, false, false, false, true, false)]
        [InlineData(false, false, false, false, false, true)] 
        public void GivenChangesToANonSupplierAssociatedField_RequestsChangesToNonSupplierAssociatedFields_ReturnsTrue(bool addChangeToName, bool addChangeToGeneral,
            bool addChangeToSystemUsageUuids, bool addNonSupplierChangeToOversight, bool addChangeToRoles, bool addChangeToExternalReferences)
        {
            var parameters = new DataProcessingRegistrationModificationParameters();
            var dprUuid = A<Guid>();
            var existingDpr = new DataProcessingRegistration(){ Uuid = dprUuid, SystemUsages = new List<ItSystemUsage>()};
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
                var existingSystemUsages = Many<ItSystemUsage>().ToList();
                existingDpr.SystemUsages = existingSystemUsages;
                parameters.SystemUsageUuids = Maybe<IEnumerable<Guid>>.Some(Many<Guid>());
            }
            if (addChangeToRoles)
            {
                parameters.Roles = Maybe<UpdatedDataProcessingRegistrationRoles>.Some(new UpdatedDataProcessingRegistrationRoles());
                parameters.Roles.Value.UserRolePairs =
                    Maybe<IEnumerable<UserRolePair>>.Some(new List<UserRolePair>()).AsChangedValue();
            }
            _dataProcessingRegistrationApplicationService.Setup(_ => _.GetByUuid(dprUuid))
                .Returns(Result<DataProcessingRegistration, OperationError>.Success(existingDpr));

            var result = _sut.RequestsChangesToNonSupplierAssociatedFields(parameters, dprUuid);

            Assert.True(result);

        }

    }
}
