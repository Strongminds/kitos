using PubSub.Application.DTOs.Request;
using PubSub.Core.DomainModel;

namespace PubSub.Application.Mapping
{
    public interface IPublishRequestMapper
    {
        Publication FromDto(PublishRequestDto dto);
    }
}
