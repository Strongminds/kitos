using Core.ApplicationServices.Authorization;
using Moq;
using Xunit;

namespace Tests.Unit.Presentation.Web.Authorization
{
    public class FieldAuthorizationModelTest
    {
        private readonly Mock<IOrganizationalUserContext> _activeUserContext;
        private readonly FieldAuthorizationModel _sut;

        public FieldAuthorizationModelTest()
        {
            _activeUserContext = new Mock<IOrganizationalUserContext>();
            _sut = new FieldAuthorizationModel(_activeUserContext.Object);
        }

        [Fact]
        public void AuthorizeUpdate_Returns_True_If_User_Is_GlobalAdmin()
        {
            _activeUserContext.Setup(x => x.IsGlobalAdmin()).Returns(true);

            var result = _sut.AuthorizeUpdate(null, null);

            Assert.True(result);

        }
    }
}
