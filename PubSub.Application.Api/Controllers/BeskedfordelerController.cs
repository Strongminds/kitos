using System.Xml;
using System.Xml.Schema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PubSub.Application.Api.UserSync.Beskedfordeler;

namespace PubSub.Application.Api.Controllers;

[ApiController]
[AllowAnonymous] // Dedicated client-certificate authentication below; bearer tokens do not grant access.
[Route("user-sync/beskedfordeler")]
public class BeskedfordelerController(IConfiguration configuration,
    IBeskedfordelerClientCertificateValidator certificates,
    IServiceProvider services,
    ILogger<BeskedfordelerController> logger) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(BeskedfordelerXmlReader.MaxBodySize)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Beskedfordeler:Enabled")) return NotFound();
        if (!Request.IsHttps || !certificates.IsValid(await HttpContext.Connection.GetClientCertificateAsync(cancellationToken)))
            return StatusCode(StatusCodes.Status403Forbidden);
        if (!configuration.GetValue<bool>("UserSync:Enabled"))
            return Reply(BeskedfordelerStatus.Unavailable);
        var mediaType = Request.ContentType?.Split(';')[0].Trim();
        if (!string.Equals(mediaType, "application/xml", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mediaType, "text/xml", StringComparison.OrdinalIgnoreCase))
            return Reply(BeskedfordelerStatus.CannotReceive);
        try
        {
            var input = await services.GetRequiredService<BeskedfordelerXmlReader>().Read(Request.Body, cancellationToken);
            return Reply(await services.GetRequiredService<BeskedfordelerReceiver>().Receive(input, cancellationToken));
        }
        catch (Exception ex) when (ex is XmlException or XmlSchemaException)
        {
            // XML diagnostics can contain payload data; never include them in logs or replies.
            return Reply(BeskedfordelerStatus.CannotReceive);
        }
        catch (DbUpdateException)
        {
            logger.LogWarning("Beskedfordeler outbox persistence failed; message was not acknowledged.");
            return Reply(BeskedfordelerStatus.Unavailable);
        }
    }

    private static ContentResult Reply(BeskedfordelerStatus status) => new()
    {
        StatusCode = StatusCodes.Status200OK,
        ContentType = "application/xml; charset=utf-8",
        Content = BeskedfordelerResponse.Serialize(status)
    };
}
