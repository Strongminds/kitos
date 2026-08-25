using Core.Abstractions.Helpers;
using Core.Abstractions.Types;
using Core.DomainModel.GDPR;
using Core.DomainModel.SupplierAssociatedFields;
using Core.DomainServices.Repositories.Organization;
using Core.DomainServices.Suppliers;
using Moq;
using System;
using System.Collections.Generic;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.DomainServices.Suppliers
{
    public class SupplierFieldDomainServiceTests : WithAutoFixture
    {
        private SupplierFieldDomainService _sut;

        private Mock<IOrganizationRepository> _organizationRepository;

        private readonly string _isOversightCompleted =
            ObjectHelper.GetPropertyPath<DataProcessingRegistration>(x => x.IsOversightCompleted);

        private readonly string _remark =
            ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightRemark);

        private readonly string _oversightReportLinkName =
            ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLinkName);

        public SupplierFieldDomainServiceTests()
        {
            _organizationRepository = new Mock<IOrganizationRepository>();
            _sut = new SupplierFieldDomainService(_organizationRepository.Object);
        }

        [Fact]
        public void IsSupplierControlled_ShouldReturnTrue_ForDefaultSupplierControlledField_WhenNoOrganizationConfiguration()
        {
            var orgUuid = SetupExpectNoConfigurations();   
            // Act
            var result = _sut.IsSupplierControlled(_isOversightCompleted, orgUuid);
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSupplierControlled_ShouldReturnFalse_ForDefaultNonSupplierControlledField_WhenNoOrganizationConfiguration()
        {
            var orgUuid = SetupExpectNoConfigurations();

            // Arrange
            var nonSupplierControlledProperty = A<string>();
            // Act
            var result = _sut.IsSupplierControlled(nonSupplierControlledProperty, orgUuid);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsSupplierControlled_ShouldOverrideSharedDefault_WhenOrganizationConfigurationExists()
        {
            var orgUuid = A<Guid>();
            var expectedConfigurations = new List<SupplierAssociatedFieldConfiguration>
            {
                new SupplierAssociatedFieldConfiguration { FieldKey = _oversightReportLinkName, ControlState = SupplierAssociatedFieldControlState.SUPPLIER }
            };
            _organizationRepository.Setup(x => x.GetSupplierAssociatedFieldConfigurations(orgUuid)).Returns(expectedConfigurations);
            
            var result = _sut.IsSupplierControlled(_oversightReportLinkName, orgUuid);
            
            Assert.True(result);
        }

        [Fact]
        public void IsSupplierControlled_ShouldOverrideSupplierDefault_WhenOrganizationConfigurationExists()
        {
            var orgUuid = A<Guid>();
            var expectedConfigurations = new List<SupplierAssociatedFieldConfiguration>
            {
                new SupplierAssociatedFieldConfiguration { FieldKey = _isOversightCompleted, ControlState = SupplierAssociatedFieldControlState.ORGANIZATION }
            };
            _organizationRepository.Setup(x => x.GetSupplierAssociatedFieldConfigurations(orgUuid)).Returns(expectedConfigurations);
            
            var result = _sut.IsSupplierControlled(_isOversightCompleted, orgUuid);
            
            Assert.False(result);
        }

        [Fact]
        public void IsSupplierControlled_ShouldReturnTrue_ForDefaultSupplierControlledField_WhenOrganizationConfigurationExistsOnlyForOtherFields()
        {
            var orgUuid = A<Guid>();
            var expectedConfigurations = new List<SupplierAssociatedFieldConfiguration>
            {
                new SupplierAssociatedFieldConfiguration { FieldKey = _remark, ControlState = SupplierAssociatedFieldControlState.ORGANIZATION }
            };
            _organizationRepository.Setup(x => x.GetSupplierAssociatedFieldConfigurations(orgUuid)).Returns(expectedConfigurations);

            var result = _sut.IsSupplierControlled(_isOversightCompleted, orgUuid);

            Assert.True(result);
        }

        [Fact]
        public void OnlySupplierFieldChanges_ShouldReturnTrue_WhenAllPropertiesAreSupplierControlled()
        {
            // Arrange
            var properties = new[]
            {
                _isOversightCompleted, _remark, _oversightReportLinkName
            };

            // Act
            var result = _sut.ContainsOnlySupplierControlledAndSharedFields(properties);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void OnlySupplierFieldChanges_ShouldReturnFalse_WhenAnyPropertyIsNotSupplierControlled()
        {
            // Arrange
            var nonSupplierControlledProperty = A<string>();
            var properties = new[]
            {
                _isOversightCompleted, nonSupplierControlledProperty
            };
            // Act
            var result = _sut.ContainsOnlySupplierControlledAndSharedFields(properties);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AnySupplierFieldChanges_ShouldReturnTrue_WhenAnyPropertyIsDefaultSupplierControlled_AndNoOrganizationConfig()
        {
            // Arrange
            var orgUuid = SetupExpectNoConfigurations();
            var nonSupplierControlledProperty = A<string>();
            var properties = new[]
            {
                nonSupplierControlledProperty, _remark
            };
            // Act
            var result = _sut.ContainsAnySupplierControlledFields(properties, orgUuid);
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AnySupplierFieldChanges_ShouldReturnFalse_WhenNoPropertyIsDefaultSupplierControlled_AndNoOrganizationConfig()
        {
            // Arrange
            var orgUuid = SetupExpectNoConfigurations();
            var nonSupplierControlledProperty1 = A<string>();
            var nonSupplierControlledProperty2 = A<string>();
            var properties = new[]
            {
                nonSupplierControlledProperty1, nonSupplierControlledProperty2
            };
            // Act
            var result = _sut.ContainsAnySupplierControlledFields(properties, orgUuid);
            // Assert
            Assert.False(result);
        }

        private Guid SetupExpectNoConfigurations()
        {
            var orgUuid = A<Guid>();
            var expectedConfigurations = Maybe<IEnumerable<SupplierAssociatedFieldConfiguration>>.None;
            _organizationRepository.Setup(x => x.GetSupplierAssociatedFieldConfigurations(orgUuid)).Returns(expectedConfigurations);
            return orgUuid;
        }
    }
}
