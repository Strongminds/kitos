using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PubSub.Application.DTOs;
using PubSub.Application.Mapping;
using PubSub.Application.Services;

namespace PubSub.Application.Controllers;

[ApiController]
[Authorize]
[Route("api/subscribe")]
public class SubscribeController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ISubscribeRequestMapper _subscribeRequestMapper;

    public SubscribeController(ISubscriptionService subscriberService, ISubscribeRequestMapper subscribeRequestMapper)
    {
        _subscriptionService = subscriberService;
        _subscribeRequestMapper = subscribeRequestMapper;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest();
        var subscriptions = _subscribeRequestMapper.FromDto(request);
        await _subscriptionService.AddSubscriptionsAsync(subscriptions);
        return NoContent();
    }
}
