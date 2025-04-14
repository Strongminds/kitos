﻿using PubSub.Application.DTOs;
using PubSub.Core.Models;

namespace PubSub.Application.Mapping;

public interface ISubscriptionMapper
{
    public SubscriptionResponseDTO ToResponseDTO(Subscription subscription);
}