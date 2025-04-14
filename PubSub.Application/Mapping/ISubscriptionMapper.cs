using PubSub.Application.DTOs;
using PubSub.Core.Models;

namespace PubSub.Application.Mapping;

public interface ISubscriptionMapper
{
    SubscriptionResponseDTO ToResponseDTO(Subscription subscription);

    IEnumerable<Subscription> FromDTO(SubscribeRequestDto dto);
}