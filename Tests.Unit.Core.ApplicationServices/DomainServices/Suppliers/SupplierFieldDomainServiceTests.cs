using Core.Abstractions.Helpers;
using Core.Abstractions.Types;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItSystemUsage;
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

        private readonly string _oversightDate =
            ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightDate);

        private readonly string _remark =
            ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightRemark);

        private readonly string _oversightReportLink =
            ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLink);

        private readonly string _oversightOptionId =
            ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightOptionId);

        private readonly string _containsAITechnology =
            ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.ContainsAITechnology);

        private readonly string _systemUsageCriticalityLevel =
            ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.SystemUsageCriticalityLevel);

        private readonly string _preriskAssessment =
            ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.preriskAssessment);

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
            var expected = new SupplierAssociatedFieldConfiguration { FieldKey = _isOversightCompleted, ControlState = SupplierAssociatedFieldControlState.ORGANIZATION };
            var orgUuid = SetupExpectConfiguration(expected);
            var result = _sut.IsSupplierControlled(_isOversightCompleted, orgUuid);
            
            Assert.False(result);
        }

        [Fact]
        public void IsSupplierControlled_ShouldReturnTrue_ForDefaultSupplierControlledField_WhenOrganizationConfigurationExistsOnlyForOtherFields()
        {
            var expected = new SupplierAssociatedFieldConfiguration { FieldKey = _remark, ControlState = SupplierAssociatedFieldControlState.ORGANIZATION };
            var orgUuid = SetupExpectConfiguration(expected);
            var expectedConfigurations = new List<SupplierAssociatedFieldConfiguration>
            {
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
        public void ContainsAnySupplierControlledFields_ShouldReturnTrue_WhenAnyPropertyIsDefaultSupplierControlled_AndNoOrganizationConfig()
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
        public void ContainsAnySupplierControlledFields_ShouldReturnFalse_WhenAllSupplierDefaultsAreOverriddenToOrganization()
        {
            // Arrange - override all 8 SUPPLIER defaults to ORGANIZATION
            var allSupplierDefaults = new[]
            {
                new SupplierAssociatedFieldConfiguration { FieldKey = _isOversightCompleted, ControlState = SupplierAssociatedFieldControlState.ORGANIZATION },
                new SupplierAssociatedFieldConfiguration { FieldKey = _oversightDate, ControlState = SupplierAssociatedFieldControlState.ORGANIZATION },
                new SupplierAssociatedFieldConfiguration { FieldKey = _remark, ControlState = SupplierAssociatedFieldControlState.ORGANIZATION },
                new SupplierAssociatedFieldConfiguration { FieldKey = _oversightReportLink, ControlState = SupplierAssociatedFieldControlState.ORGANIZATION },
                new SupplierAssociatedFieldConfiguration { FieldKey = _oversightOptionId, ControlState = SupplierAssociatedFieldControlState.ORGANIZATION },
                new SupplierAssociatedFieldConfiguration { FieldKey = _containsAITechnology, ControlState = SupplierAssociatedFieldControlState.ORGANIZATION },
                new SupplierAssociatedFieldConfiguration { FieldKey = _systemUsageCriticalityLevel, ControlState = SupplierAssociatedFieldControlState.ORGANIZATION },
                new SupplierAssociatedFieldConfiguration { FieldKey = _preriskAssessment, ControlState = SupplierAssociatedFieldControlState.ORGANIZATION }
            };
            var orgUuid = SetupExpectConfiguration(allSupplierDefaults);
            var properties = new[] { _isOversightCompleted, _oversightDate, _remark, _oversightReportLink, _oversightOptionId, _containsAITechnology, _systemUsageCriticalityLevel, _preriskAssessment };
            // Act
            var result = _sut.ContainsAnySupplierControlledFields(properties, orgUuid);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ContainsAnySupplierControlledFields_ShouldReturnFalse_WhenNoPropertyIsDefaultSupplierControlled_AndNoOrganizationConfig()
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

        [Fact]
        public void ContainsAnySupplierControlledFields_ShouldOverrideDefaultWithTrue_WhenAnyPropertyIsSupplierControlledInOrganizationConfig()
        {
            // Arrange
            var expected = new SupplierAssociatedFieldConfiguration { FieldKey = _oversightReportLinkName, ControlState = SupplierAssociatedFieldControlState.SUPPLIER };
            var orgUuid = SetupExpectConfiguration(expected);
            var nonSupplierControlledProperty = A<string>();
            var properties = new[]
            {
                nonSupplierControlledProperty, _oversightReportLinkName
            };
            // Act
            var result = _sut.ContainsAnySupplierControlledFields(properties, orgUuid);
            // Assert
            Assert.True(result);
        }

        private Guid SetupExpectConfiguration(params SupplierAssociatedFieldConfiguration[] configurations) {
            var orgUuid = A<Guid>();
            var expectedConfigurations = new List<SupplierAssociatedFieldConfiguration>(configurations);
            _organizationRepository.Setup(x => x.GetSupplierAssociatedFieldConfigurations(orgUuid)).Returns(expectedConfigurations);
            return orgUuid;
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
