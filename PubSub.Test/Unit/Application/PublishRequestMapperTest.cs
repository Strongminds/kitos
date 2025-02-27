using AutoFixture;
using PubSub.Application.DTOs;
using PubSub.Application.Mapping;

namespace PubSub.Test.Unit.Application
{
    public class PublishRequestMapperTest
    {
        private Fixture _fixture;
        private PublishRequestMapper _sut;

        public PublishRequestMapperTest()
        {
            _fixture = new Fixture();
            _sut = new PublishRequestMapper();
        }

        [Fact]
        public void Can_Map_From_Dto()
        {
            var dto = _fixture.Create<PublishRequestDto>();

            var publication = _sut.FromDto(dto);

            Assert.Equal(dto.Topic, publication.Topic.Name);
            Assert.Equal(dto.Message, publication.Message);
        }
    }
}
