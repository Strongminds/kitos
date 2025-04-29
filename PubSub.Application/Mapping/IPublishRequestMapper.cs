using PubSub.Application.DTOs;
using PubSub.Core.DomainModel;

namespace PubSub.Application.Mapping
{
    public interface IPublishRequestMapper
    {
        Publication FromDto(PublishRequestDto dto);
    }
}
