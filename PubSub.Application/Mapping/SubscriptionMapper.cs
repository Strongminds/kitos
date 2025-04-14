using PubSub.Application.DTOs;
using PubSub.Core.Models;

namespace PubSub.Application.Mapping;

public class SubscriptionMapper : ISubscriptionMapper
{
    public SubscriptionResponseDTO ToResponseDTO(Subscription subscription)
    {
        return new SubscriptionResponseDTO
        {
            Uuid = subscription.Uuid,
            CallbackUrl = subscription.Callback,
            Topic = subscription.Topic
        };
    }

    public IEnumerable<Subscription> FromDTO(SubscribeRequestDto dto)
    {
        return dto.Topics.Select(topicName => new Subscription(dto.Callback.AbsoluteUri, topicName));
    }
}