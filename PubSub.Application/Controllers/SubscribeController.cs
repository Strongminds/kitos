using Microsoft.AspNetCore.Mvc;
using PubSub.Application.Subscribe;
using PubSub.Core.Models;
using PubSub.Core.Services;

namespace PubSub.Application.Controllers;

[ApiController]
[Route("api/subscribe")]
public class SubscribeController : ControllerBase
{
    private readonly ISubscribeLoopHostedService _subscribeLoopHostedService;
    private readonly ISubscriberService _subscriberService;

    public SubscribeController(ISubscribeLoopHostedService subscribeLoopHostedService, ISubscriberService subscriberService)
    {
        _subscribeLoopHostedService = subscribeLoopHostedService;
        _subscriberService = subscriberService;
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] SubscriptionRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest();
        var subscriptions = new List<Subscription>() { new() { Callback = request.Callback, Topics = request.Queues } };
        await _subscriberService.SubscribeToQueuesAsync(subscriptions);
        //await _subscribeLoopHostedService.UpdateSubscriptions(subscriptions);
        return Ok();
    }
}
