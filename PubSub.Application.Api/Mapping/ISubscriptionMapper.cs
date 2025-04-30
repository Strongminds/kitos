using PubSub.Application.Api.DTOs.Request;
using PubSub.Application.Api.DTOs.Response;
using PubSub.Core.ApplicationServices.Models;
using PubSub.Core.DomainModel;

namespace PubSub.Application.Api.Mapping;

public interface ISubscriptionMapper
{
    SubscriptionResponseDTO ToResponseDTO(Subscription subscription);

    IEnumerable<CreateSubscriptionParameters> FromDTO(SubscribeRequestDto dto);
}