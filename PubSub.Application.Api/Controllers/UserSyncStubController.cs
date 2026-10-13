using UserSync.Transport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PubSub.Application.Api.UserSync;
using PubSub.Core.DomainModel.UserSync;

namespace PubSub.Application.Api.Controllers;

[ApiController]
[Authorize(Policy = Constants.Config.Validation.CanPublishPolicy)]
[Route("user-sync/stub")]
public class UserSyncStubController(UserChangeIngress ingress, IWebHostEnvironment environment, IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Send(UserDeletionEvent message, CancellationToken cancellationToken)
    {
        var deployment = Environment.GetEnvironmentVariable(Constants.Config.Environment.CurrentEnvironment);
        if (!(environment.IsDevelopment() || environment.IsEnvironment("Local") || environment.IsEnvironment("Test")) ||
            string.Equals(deployment, "Production", StringComparison.OrdinalIgnoreCase) ||
            !configuration.GetValue<bool>("UserSync:EnableStub") || !configuration.GetValue<bool>("UserSync:Enabled")) return NotFound();
        if (!message.IsValid()) return BadRequest("Invalid deletion event.");
        try
        {
            var delivery = await ingress.Enqueue(message, cancellationToken);
            return Accepted(new { delivery.ExternalMessageId, QueueAccepted = delivery.AcceptedAt != null });
        }
        catch (UserChangeConflictException) { return Conflict("Message ID already belongs to another event."); }
    }
}
