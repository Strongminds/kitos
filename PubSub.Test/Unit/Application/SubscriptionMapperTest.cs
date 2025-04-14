using AutoFixture;
using PubSub.Application.DTOs;
using PubSub.Application.Mapping;
using PubSub.Test.Base.Tests.Toolkit.Patterns;

namespace PubSub.Test.Unit.Application
{
    public class SubscriptionMapperTest : WithAutoFixture
    {
        private SubscriptionMapper _sut;

        public SubscriptionMapperTest()
        {
            _sut = new SubscriptionMapper();
        }

        [Fact]
        public void Can_Map_From_Dto()
        {
            var dto = A<SubscribeRequestDto>();

            var subscriptions = _sut.FromDTO(dto);

            Assert.All(dto.Topics, topic => Assert.Contains(topic, subscriptions.Select(x => x.Topic)));
        }
    }
}
