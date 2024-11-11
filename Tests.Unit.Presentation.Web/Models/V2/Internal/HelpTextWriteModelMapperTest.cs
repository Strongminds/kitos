using Moq;
using Presentation.Web.Controllers.API.V2.Internal.Mapping;
using Presentation.Web.Infrastructure.Model.Request;
using Presentation.Web.Models.API.V2.Internal.Request;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Presentation.Web.Models.V2.Internal
{
    public class HelpTextWriteModelMapperTest: WithAutoFixture
    {

        private readonly HelpTextWriteModelMapper _sut;
        private readonly Mock<ICurrentHttpRequest> _request;

        public HelpTextWriteModelMapperTest()
        {
            var request = new Mock<ICurrentHttpRequest>();
            _sut = new HelpTextWriteModelMapper(request.Object);
        }

        [Fact]
        public void Can_Map_Create_Parameters()
        {
            var dto = A<HelpTextCreateRequestDTO>();

            var result = _sut.ToCreateParameters(dto);

            Assert.Equivalent(dto, result);
        }
    }
}
