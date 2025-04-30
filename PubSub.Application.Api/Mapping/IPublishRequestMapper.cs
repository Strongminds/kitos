using PubSub.Application.Api.DTOs.Request;
using PubSub.Core.DomainModel;

namespace PubSub.Application.Api.Mapping
{
    public interface IPublishRequestMapper
    {
        Publication FromDto(PublishRequestDto dto);
    }
}
