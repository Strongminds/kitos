using Core.Abstractions.Helpers;
using Core.DomainModel.GDPR;
using Core.DomainServices.Suppliers;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.DomainServices.Suppliers
{
    public class SupplierFieldDomainServiceTests : WithAutoFixture
    {
        private SupplierFieldDomainService _sut;

        private readonly string _isOversightCompleted =
            ObjectHelper.GetPropertyPath<DataProcessingRegistration>(x => x.IsOversightCompleted);

        private readonly string _remark =
            ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightRemark);

        private readonly string _oversightReportLinkName =
            ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLinkName);

        public SupplierFieldDomainServiceTests()
        {
            _sut = new SupplierFieldDomainService();
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
            var result = _sut.OnlySupplierFieldChanges(properties);

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
            var result = _sut.OnlySupplierFieldChanges(properties);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsSupplierControlled_ShouldReturnTrue_ForSupplierControlledField()
        {
            // Act
            var result = _sut.IsSupplierControlled(_isOversightCompleted);
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSupplierControlled_ShouldReturnFalse_ForNonSupplierControlledField()
        {
            // Arrange
            var nonSupplierControlledProperty = A<string>();
            // Act
            var result = _sut.IsSupplierControlled(nonSupplierControlledProperty);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AnySupplierFieldChanges_ShouldReturnTrue_WhenAnyPropertyIsSupplierControlled()
        {
            // Arrange
            var nonSupplierControlledProperty = A<string>();
            var properties = new[]
            {
                nonSupplierControlledProperty, _remark
            };
            // Act
            var result = _sut.AnySupplierFieldChanges(properties);
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AnySupplierFieldChanges_ShouldReturnFalse_WhenNoPropertyIsSupplierControlled()
        {
            // Arrange
            var nonSupplierControlledProperty1 = A<string>();
            var nonSupplierControlledProperty2 = A<string>();
            var properties = new[]
            {
                nonSupplierControlledProperty1, nonSupplierControlledProperty2
            };
            // Act
            var result = _sut.AnySupplierFieldChanges(properties);
            // Assert
            Assert.False(result);
        }
    }
}
