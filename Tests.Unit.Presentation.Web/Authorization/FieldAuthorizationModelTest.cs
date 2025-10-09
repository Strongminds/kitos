using System.Collections.Generic;
using System.Linq;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Model;
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
            IsUserGlobalAdmin(true);
            var result = _sut.AuthorizeUpdate(null, null);

            Assert.True(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenRequestsNoChangesToSupplierFields_AuthorizeUpdate_Returns_AllowModify(bool expected)
        {
            IsUserGlobalAdmin(false);
            var entity = new Mock<IEntityOwnedByOrganization>();
            RequestsChangesToSupplierAssociatedFieldsReturns(false, new Mock<ISupplierAssociatedEntityUpdateParameters>());
            _authorizationContext.Setup(_ => _.AllowModify(entity.Object)).Returns(expected);

            var result = _sut.AuthorizeUpdate(entity.Object,
                new Mock<ISupplierAssociatedEntityUpdateParameters>().Object);

            Assert.Equal(expected, result);
            _authorizationContext.Verify(_ => _.AllowModify(entity.Object));

        }

        [Fact]
        public void GivenRequestsChangesToSupplierFieldsAndUserDoesNotHaveSupplierApiAccess_AuthorizeUpdate_Returns_False()
        {
            IsUserGlobalAdmin(false);
            var entity = new Mock<IEntityOwnedByOrganization>();
            var parameters = new Mock<ISupplierAssociatedEntityUpdateParameters>();
            RequestsChangesToSupplierAssociatedFieldsReturns(true, parameters);
            SetupDoesUserHaveSupplierApiAccess(false, entity);

            var result = _sut.AuthorizeUpdate(entity.Object, parameters.Object);
            
            Assert.False(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenUserHasSupplierApiAccess_AuthorizeUpdate_ReturnValue_Depends_On_RequestsChangesToNonSupplierFields_And_AllowModify(bool requestsNonSupplierChanges)
        {
            IsUserGlobalAdmin(false);
            var entity = new Mock<IEntityOwnedByOrganization>();
            var parameters = new Mock<ISupplierAssociatedEntityUpdateParameters>();
            RequestsChangesToSupplierAssociatedFieldsReturns(true, parameters);
            _supplierAssociatedFieldsService.Setup(_ => _.RequestsChangesToNonSupplierAssociatedFields(parameters.Object, It.IsAny<int>())).Returns(requestsNonSupplierChanges);
            SetupDoesUserHaveSupplierApiAccess(true, entity);
            var allowModify = A<bool>();
            _authorizationContext.Setup(_ => _.AllowModify(entity.Object)).Returns(allowModify);

            var result = _sut.AuthorizeUpdate(entity.Object, parameters.Object);

            if (requestsNonSupplierChanges)
            {
                Assert.False(result);
            }
            else
            {
                Assert.Equal(allowModify, result);
                _authorizationContext.Verify(_ => _.AllowModify(entity.Object));
            }
        }

        private void IsUserGlobalAdmin(bool value)
        {
            _activeUserContext.Setup(x => x.IsGlobalAdmin()).Returns(value);
        }

        private void RequestsChangesToSupplierAssociatedFieldsReturns(bool value, Mock<ISupplierAssociatedEntityUpdateParameters> parameters)
        {
            _supplierAssociatedFieldsService.Setup(_ => _.RequestsChangesToSupplierAssociatedFields(parameters.Object)).Returns(true);
        }

        private void SetupDoesUserHaveSupplierApiAccess(bool value, Mock<IEntityOwnedByOrganization> entity)
        {
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
                    HasApiAccess = value
                }
            }.AsQueryable());
        }
    }
}
