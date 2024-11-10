

using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Authorization;
using Core.ApplicationServices.HelpTexts;
using Core.ApplicationServices.Model.HelpTexts;
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
        private Mock<IOrganizationalUserContext> _userContext;

        public HelpTextServiceTest()
        {
            _helpTextsRepository = new Mock<IGenericRepository<HelpText>>();
            _userContext = new Mock<IOrganizationalUserContext>();
            _sut = new HelpTextService(_helpTextsRepository.Object, _userContext.Object);
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

        [Fact]
        public void Create_Help_Text_Returns_Forbidden_If_Not_Global_Admin()
        {
            _userContext.Setup(_ => _.IsGlobalAdmin()).Returns(false);

            var parameters = A<HelpTextCreateParameters>();

            var result = _sut.CreateHelpText(parameters);

            Assert.True(result.Failed);
            Assert.Equal(OperationFailure.Forbidden, result.Error.FailureType);
        }

        [Fact]
        public void Can_Create_Help_Text()
        {
            _userContext.Setup(_ => _.IsGlobalAdmin()).Returns(true);

            var parameters = A<HelpTextCreateParameters>();

            var result = _sut.CreateHelpText(parameters);

            Assert.True(result.Ok);
            var helpText = result.Value;
            Assert.Equal(parameters.Description, helpText.Description);
            Assert.Equal(parameters.Title, helpText.Title);
            Assert.Equal(parameters.Key, helpText.Key);
        }


    }
}
