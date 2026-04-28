using Core.DomainModel.ItSystem.DataTypes;
using Core.DomainModel.ItSystemUsage;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.Model
{
    public class ItSystemUsageCriticalityInfoTest: WithAutoFixture
    {
        [Fact]
        public void UpdateIsBusinessCritical_ShouldUpdateLastChanged()
        {
            var sut = new ItSystemUsageCriticalityInfo();
            Assert.Null(sut.LastChanged);

            sut.UpdateIsBusinessCritical(A<DataOptions?>());
            Assert.NotNull(sut.LastChanged);
        }

        [Fact]
        public void UpdateIsSociallyCritical_ShouldUpdateLastChanged()
        {
            var sut = new ItSystemUsageCriticalityInfo();
            Assert.Null(sut.LastChanged);

            sut.UpdateIsSociallyCritical(A<DataOptions?>());
            Assert.NotNull(sut.LastChanged);
        }
    }
}
