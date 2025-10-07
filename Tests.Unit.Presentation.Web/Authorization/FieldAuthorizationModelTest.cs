using System.Collections.Generic;
using System.Linq;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Model;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.DomainModel;
using Core.DomainModel.Organization;
using Core.DomainServices;
using Moq;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Presentation.Web.Authorization
{
    public class FieldAuthorizationModelTest: WithAutoFixture
    {
        private readonly Mock<IOrganizationalUserContext> _activeUserContext;
        private readonly FieldAuthorizationModel _sut;
        private readonly Mock<ISupplierAssociatedFieldsService> _supplierAssociatedFieldsService;
        private readonly Mock<IAuthorizationContext> _authorizationContext;
        private readonly Mock<IUserRepository> _userRepository;

        public FieldAuthorizationModelTest()
        {
            _activeUserContext = new Mock<IOrganizationalUserContext>();
            _supplierAssociatedFieldsService = new Mock<ISupplierAssociatedFieldsService>();
            _authorizationContext = new Mock<IAuthorizationContext>();
            _userRepository = new Mock<IUserRepository>();
            _sut = new FieldAuthorizationModel(_activeUserContext.Object, _supplierAssociatedFieldsService.Object, _authorizationContext.Object, _userRepository.Object);
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
            var entity = new Mock<IEntityOwnedByOrganization>();
            _supplierAssociatedFieldsService.Setup(_ => _.RequestsChangesToSupplierAssociatedFields(It.IsAny<DataProcessingRegistrationModificationParameters>())).Returns(false);
            _authorizationContext.Setup(_ => _.AllowModify(entity.Object)).Returns(expected);

            var result = _sut.AuthorizeUpdate(entity.Object,
                new Mock<ISupplierAssociatedEntityUpdateParameters>().Object);

            Assert.Equal(expected, result);
            _authorizationContext.Verify(_ => _.AllowModify(entity.Object));

        }

        [Fact]
        public void GivenRequestsChangesToSupplierFieldsAndUserDoesNotHaveSupplierApiAccess_AuthorizeUpdate_Returns_False()
        {
            _activeUserContext.Setup(x => x.IsGlobalAdmin()).Returns(false);
            var entity = new Mock<IEntityOwnedByOrganization>();
            var dprParams = new DataProcessingRegistrationModificationParameters();
            _supplierAssociatedFieldsService.Setup(_ => _.RequestsChangesToSupplierAssociatedFields(dprParams)).Returns(true);
            var supplierId = A<int>();
            var userId = A<int>();
            entity.SetupGet(e => e.Organization).Returns(new Organization
            {
                Suppliers = new List<OrganizationSupplier>
                {
                    new() { SupplierId = supplierId }
                }
            });
            _activeUserContext.Setup(_ => _.UserId).Returns(userId);
            _userRepository.Setup(_ => _.GetUsersInOrganization(supplierId)).Returns(new List<User>()
            {
                new()
                {
                    Id = userId,
                    HasApiAccess = false
                }
            }.AsQueryable());

            var result = _sut.AuthorizeUpdate(entity.Object, dprParams);
            
            Assert.False(result);
        }
    }
}
