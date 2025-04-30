using PubSub.Application.Api.DTOs.Request;
using PubSub.Core.DomainModel;

namespace PubSub.Application.Api.Mapping
{
    public class PublishRequestMapper : IPublishRequestMapper
    {
        public Publication FromDto(PublishRequestDto dto)
        {
            return new Publication(new Topic(dto.Topic), dto.Payload);
        }
    }
}
