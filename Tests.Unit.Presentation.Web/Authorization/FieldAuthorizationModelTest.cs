using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Model;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.DomainModel;
using Moq;
using Xunit;

namespace Tests.Unit.Presentation.Web.Authorization
{
    public class FieldAuthorizationModelTest
    {
        private readonly Mock<IOrganizationalUserContext> _activeUserContext;
        private readonly FieldAuthorizationModel _sut;
        private readonly Mock<ISupplierAssociatedFieldsService> _supplierAssociatedFieldsService;
        private readonly Mock<IAuthorizationContext> _authorizationContext;

        public FieldAuthorizationModelTest()
        {
            _activeUserContext = new Mock<IOrganizationalUserContext>();
            _supplierAssociatedFieldsService = new Mock<ISupplierAssociatedFieldsService>();
            _authorizationContext = new Mock<IAuthorizationContext>();
            _sut = new FieldAuthorizationModel(_activeUserContext.Object, _supplierAssociatedFieldsService.Object, _authorizationContext.Object);
        }

        [Fact]
        public void GivenUserIsGlobalAdmin_AuthorizeUpdate_Returns_True()
        {
            _activeUserContext.Setup(x => x.IsGlobalAdmin()).Returns(true);

            var result = _sut.AuthorizeUpdate(null, null);

            Assert.True(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenRequestsNoChangesToSupplierFields_AuthorizeUpdate_Returns_Result_Of_AllowModify(bool expected)
        {
            var entity = new Mock<IEntity>();
            _supplierAssociatedFieldsService.Setup(_ => _.RequestsChangesToSupplierAssociatedFields(It.IsAny<DataProcessingRegistrationModificationParameters>())).Returns(false);
            _authorizationContext.Setup(_ => _.AllowModify(entity.Object)).Returns(expected);

            var result = _sut.AuthorizeUpdate(entity.Object,
                new Mock<ISupplierAssociatedEntityUpdateParameters>().Object);

            Assert.Equal(expected, result);
            _authorizationContext.Verify(_ => _.AllowModify(entity.Object));

        }

        [Fact]
        public void GivenRequestsChangesOnlyToSupplierFields_AuthorizeUpdate_Returns_True()
        {
            _activeUserContext.Setup(x => x.IsGlobalAdmin()).Returns(false);
            var entity = new Mock<IEntity>();
            var dprParams = new DataProcessingRegistrationModificationParameters();
            _supplierAssociatedFieldsService.Setup(_ => _.RequestsChangesToSupplierAssociatedFields(dprParams)).Returns(true);
            _supplierAssociatedFieldsService.Setup(_ => _.RequestsChangesToNonSupplierAssociatedFields(dprParams, It.IsAny<int>())).Returns(false);
            
            var result = _sut.AuthorizeUpdate(entity.Object, dprParams);
            
            Assert.False(result);
        }
    }
}
