using Microsoft.AspNetCore.Mvc;

namespace PubSub.Application.Controllers;

[ApiController]
[Route("api/subscribe")]
public class SubscribeController : ControllerBase
{
    private readonly ISubscribeLoopHostedService _subscribeLoopHostedService;

    public SubscribeController(ISubscribeLoopHostedService subscribeLoopHostedService)
    {
        _subscribeLoopHostedService = subscribeLoopHostedService;
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] SubscriptionRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest();
        var subscriptions = new List<Subscription>() { new Subscription { Callback = request.Callback, Queues = request.Queues } };
        await _subscribeLoopHostedService.UpdateSubscriptions(subscriptions);
        return Ok();
    }
}
