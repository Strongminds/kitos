﻿using PubSub.Application.DTOs;
using PubSub.Application.Models;
using PubSub.Core.DomainModel;

namespace PubSub.Application.Mapping;

public interface ISubscriptionMapper
{
    SubscriptionResponseDTO ToResponseDTO(Subscription subscription);

    IEnumerable<CreateSubscriptionParameters> FromDTO(SubscribeRequestDto dto);
}