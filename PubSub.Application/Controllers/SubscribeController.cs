using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PubSub.Application.DTOs;
using PubSub.Core.Models;
using PubSub.Core.Services.Subscribe;
using Swashbuckle.AspNetCore.Annotations;

namespace PubSub.Application.Controllers;

[ApiController]
[Authorize]
[Route("api/subscribe")]
public class SubscribeController : ControllerBase
{
    private readonly ISubscriberService _subscriberService;

    public SubscribeController(ISubscriberService subscriberService)
    {
        _subscriberService = subscriberService;
    }

    [HttpPost]
    [SwaggerResponse((int)HttpStatusCode.NoContent)]
    [SwaggerResponse((int)HttpStatusCode.BadRequest)]
    [SwaggerResponse((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> Subscribe([FromBody] SubscriptionRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest();
        var subscriptions = new List<Subscription>() { new() { Callback = request.Callback, Topics = request.Queues } };
        await _subscriberService.AddSubscriptionsAsync(subscriptions);
        return NoContent();
    }
}
