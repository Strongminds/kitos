

using System.Collections.Generic;
using System.Linq;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.HelpTexts;
using Core.DomainModel;
using Core.DomainServices;
using Moq;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.ApplicationServices.HelpTexts
{
    public class HelpTextServiceTest: WithAutoFixture
    {
        private HelpTextService _sut;
        private Mock<IGenericRepository<HelpText>> _helpTextsRepository;
        private Mock<IAuthorizationContext> _authorizationContext;

        public HelpTextServiceTest()
        {
            _helpTextsRepository = new Mock<IGenericRepository<HelpText>>();
            _sut = new HelpTextService(_helpTextsRepository.Object);
        }

        [Fact]
        public void Can_Get_Help_Texts()
        {
            var expected = new HelpText()
            {
                Description = A<string>(),
                Key = A<string>(),
                Title = A<string>()
            };
            _helpTextsRepository.Setup(_ => _.AsQueryable()).Returns(new List<HelpText>(){expected}.AsQueryable());

            var result = _sut.GetHelpTexts();

            Assert.True(result.Ok);
            var helpText = result.Value.FirstOrDefault();
            Assert.NotNull(helpText);
            Assert.Equivalent(expected, helpText);
        }
    }
}
