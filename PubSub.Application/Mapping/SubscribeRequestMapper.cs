using PubSub.Application.DTOs;
using PubSub.Core.Models;

namespace PubSub.Application.Mapping
{
    public class SubscribeRequestMapper : ISubscribeRequestMapper
    {
        public IEnumerable<Subscription> FromDto(SubscribeRequestDto dto)
        {
            return dto.Topics.Select(topicName => new Subscription(dto.Callback, topicName));
        }
    }
}
