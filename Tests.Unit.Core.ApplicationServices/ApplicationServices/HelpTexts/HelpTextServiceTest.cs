

using Core.ApplicationServices.HelpTexts;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.ApplicationServices.HelpTexts
{
    public class HelpTextServiceTest: WithAutoFixture
    {
        private HelpTextService _sut;

        public HelpTextServiceTest()
        {
            _sut = new HelpTextService();
        }

        [Fact]
        public void Can_Get_Help_Texts()
        {
            //todo test
            var result = _sut.GetHelpTexts();
        }
    }
}
