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
        public void GivenSupplierApiUser_RequestsChangesToNonSupplierFields_AuthorizeUpdate_Returns_AllowModifyResult(bool allowModifyResult)
        {
            IsUserGlobalAdmin(false);
            var parameters = new Mock<ISupplierAssociatedEntityUpdateParameters>();
            var entity = SetupEntityWithId();

            RequestsChangesToNonSupplierAssociatedFieldsReturns(true, parameters, entity.Object.Id);
            SetupDoesUserHaveSupplierApiAccess(true, entity);
            SetupAllowModifyReturns(allowModifyResult, entity.Object);

            var result = _sut.AuthorizeUpdate(entity.Object, parameters.Object);

            Assert.Equal(allowModifyResult, result);
        }

        [Fact]
        public void GivenSupplierApiUser_DoesNotRequestChangesToNonSupplierFields_AuthorizeUpdate_Returns_True()
        {
            IsUserGlobalAdmin(false);
            var parameters = new Mock<ISupplierAssociatedEntityUpdateParameters>();
            var entity = SetupEntityWithId();
            RequestsChangesToNonSupplierAssociatedFieldsReturns(false, parameters, entity.Object.Id);
            SetupDoesUserHaveSupplierApiAccess(true, entity);

            var result = _sut.AuthorizeUpdate(entity.Object, parameters.Object);

            Assert.True(result);
        }

        [Fact]
        public void GivenNonSupplierApiUser_RequestsChangesToSupplierFields_AuthorizeUpdate_Returns_False()
        {
            IsUserGlobalAdmin(false);
            var parameters = new Mock<ISupplierAssociatedEntityUpdateParameters>();
            var entity = SetupEntityWithId();

            RequestsChangesToSupplierAssociatedFieldsReturns(true, parameters);
            SetupDoesUserHaveSupplierApiAccess(false, entity);

            var result = _sut.AuthorizeUpdate(entity.Object, parameters.Object);

            Assert.False(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenNonSupplierApiUser_DoesNotRequestChangesToSupplierFields_AuthorizeUpdate_Returns_AllowModifyResult(bool allowModifyResult)
        {
            IsUserGlobalAdmin(false);
            var parameters = new Mock<ISupplierAssociatedEntityUpdateParameters>();
            var entity = SetupEntityWithId();
            RequestsChangesToNonSupplierAssociatedFieldsReturns(false, parameters, entity.Object.Id);
            SetupDoesUserHaveSupplierApiAccess(false, entity);
            SetupAllowModifyReturns(allowModifyResult, entity.Object);

            var result = _sut.AuthorizeUpdate(entity.Object, parameters.Object);

            Assert.Equal(allowModifyResult, result);
        }

        private Mock<IEntityOwnedByOrganization> SetupEntityWithId()
        {
            var entityId = A<int>();
            var entity = new Mock<IEntityOwnedByOrganization>();
            entity.Setup(_ => _.Id).Returns(entityId);
            return entity;
        }

        private void SetupAllowModifyReturns(bool result, IEntityOwnedByOrganization entity)
        {
            _authorizationContext.Setup(_ => _.AllowModify(entity)).Returns(result);
        }

        private void IsUserGlobalAdmin(bool value)
        {
            _activeUserContext.Setup(x => x.IsGlobalAdmin()).Returns(value);
        }

        private void RequestsChangesToSupplierAssociatedFieldsReturns(bool value, Mock<ISupplierAssociatedEntityUpdateParameters> parameters)
        {
            _supplierAssociatedFieldsService.Setup(_ => _.RequestsChangesToSupplierAssociatedFields(parameters.Object)).Returns(true);
        }

        private void RequestsChangesToNonSupplierAssociatedFieldsReturns(bool value, Mock<ISupplierAssociatedEntityUpdateParameters> parameters, int entityId)
        {
            _supplierAssociatedFieldsService.Setup(_ => _.RequestsChangesToNonSupplierAssociatedFields(parameters.Object, entityId)).Returns(value);
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
