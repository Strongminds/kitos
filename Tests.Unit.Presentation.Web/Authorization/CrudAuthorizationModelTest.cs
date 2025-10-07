using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.Model;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.DomainModel;
using Core.DomainModel.GDPR;
using Moq;
using Xunit;

namespace Tests.Unit.Presentation.Web.Authorization
{
    public class CrudAuthorizationModelTest
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AuthorizeUpdate_ReturnsResultOf_AuthorizationContext_CanModify(bool expected)
        {
            var entity = new Mock<IEntityOwnedByOrganization>();
            var parameters = new Mock<ISupplierAssociatedEntityUpdateParameters>();
            var authorizationContext = new Mock<IAuthorizationContext>();
            authorizationContext.Setup(_ => _.AllowModify(It.IsAny<IEntity>())).Returns(expected);
            var sut = new CrudAuthorizationModel(authorizationContext.Object);

            var result = sut.AuthorizeUpdate(entity.Object, parameters.Object);

            Assert.Equal(expected, result);
            authorizationContext.Verify(_ => _.AllowModify(entity.Object));

        }
    }
}
