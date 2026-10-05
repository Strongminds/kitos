using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.ApplicationServices.Users;
using Presentation.Web.Models.API.V2.Integration.Request;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Infrastructure.Attributes;
using Newtonsoft.Json;

namespace Presentation.Web.Controllers.API.V2.Integration;

/// <summary>Receives user changes from a KITOS-authenticated PubSub account.</summary>
[Route("api/v2/integrations/fk-organisation/user-changes")]
[RequirePubSubUser]
public class ExternalUserChangeIngestionController(ExternalUserChangeIngestionService service)
    : IntegrationApiV2Controller
{
    /// <summary>Stores a user change event.</summary>
    /// <remarks>
    /// Returns 409 Conflict (permanent; dead-lettered by the PubSub delivery worker) when the organization's
    /// FK Organisation users connection is not enabled, or when the message ID belongs to a different event.
    /// Identical replays for connected organizations return 200 with the existing change.
    /// </remarks>
    [HttpPost]
    [RequestSizeLimit(16384)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);

        ExternalUserChangePublication? publication;
        try
        {
            publication = JsonConvert.DeserializeObject<ExternalUserChangePublication>(body, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                MissingMemberHandling = MissingMemberHandling.Error
            });
        }
        catch (JsonException) { return BadRequest("Invalid event envelope."); }
        if (publication?.Payload == null) return BadRequest("Payload is required.");
        var result = await service.Ingest(publication.Payload, cancellationToken);
        return result.Match(change => Ok(new { change.Uuid }), FromOperationError);
    }
}
