using AutoFixture;
using PubSub.Application.DTOs;
using PubSub.Application.Mapping;

namespace PubSub.Test.Unit.Application
{
    public class SubscribeRequestMapperTest
    {
        private Fixture _fixture;
        private SubscribeRequestMapper _sut;

        public SubscribeRequestMapperTest()
        {
            _fixture = new Fixture();
            _sut = new SubscribeRequestMapper();
        }

        [Fact]
        public void Can_Map_From_Dto()
        {
            var dto = _fixture.Create<SubscribeRequestDto>();

            var subscription = _sut.FromDto(dto);

            Assert.Equal(dto.Callback, subscription.Callback);
            Assert.Equal(dto.Topics, subscription.Topics.Select(t => t.Name));
        }
    }
}
