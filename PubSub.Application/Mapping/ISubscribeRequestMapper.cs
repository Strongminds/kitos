using PubSub.Application.DTOs;
using PubSub.Core.Models;

namespace PubSub.Application.Mapping
{
    public interface ISubscribeRequestMapper
    {
        IEnumerable<Subscription> FromDto(SubscribeRequestDto dto);
    }
}
