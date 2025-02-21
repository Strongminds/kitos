using Microsoft.AspNetCore.Mvc;

namespace PubSub.Application.Controllers;

[ApiController]
[Route("subscribe")]
public class SubscribeController : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> Subscribe(IList<Subscription> subscriptions)
    {
        if (!ModelState.IsValid) return BadRequest("Invalid request object provided.");

        return Ok();
    }
}
