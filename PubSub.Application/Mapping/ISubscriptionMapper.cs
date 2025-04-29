using PubSub.Application.DTOs.Request;
using PubSub.Application.DTOs.Response;
using PubSub.Application.Models;
using PubSub.Core.DomainModel;

namespace PubSub.Application.Mapping;

public interface ISubscriptionMapper
{
    SubscriptionResponseDTO ToResponseDTO(Subscription subscription);

    IEnumerable<CreateSubscriptionParameters> FromDTO(SubscribeRequestDto dto);
}