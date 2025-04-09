using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PubSub.Application.DTOs;
using PubSub.Application.Mapping;
using PubSub.Core.Models;
using PubSub.Core.Services.Subscribe;


namespace PubSub.Application.Controllers;

[ApiController]
[Authorize]
[Route("api/subscribe")]
public class SubscribeController : ControllerBase
{
    private readonly ISubscriberService _subscriberService;
    private readonly ISubscribeRequestMapper _subscribeRequestMapper;

    public SubscribeController(ISubscriberService subscriberService, ISubscribeRequestMapper subscribeRequestMapper)
    {
        _subscriberService = subscriberService;
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
        await _subscriberService.AddSubscriptionsAsync(subscriptions);
        return NoContent();
    }
}
