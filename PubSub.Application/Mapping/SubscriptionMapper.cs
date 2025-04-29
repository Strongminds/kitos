using PubSub.Application.DTOs;
using PubSub.Application.DTOs.Request;
using PubSub.Application.Models;
using PubSub.Core.DomainModel;

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

    public IEnumerable<CreateSubscriptionParameters> FromDTO(SubscribeRequestDto dto)
    {
        return dto.Topics.Select(topic => new CreateSubscriptionParameters(dto.Callback.AbsoluteUri, topic)).ToList();
    }
}