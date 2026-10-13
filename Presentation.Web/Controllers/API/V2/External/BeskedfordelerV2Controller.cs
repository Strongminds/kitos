using System;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.STS.Beskedfordeler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Presentation.Web.Controllers.API.V2.External;

[Route("api/v2/integration/beskedfordeler")]
public sealed class BeskedfordelerV2Controller(
    IConfiguration configuration,
    IBeskedfordelerClientCertificateValidator certificates,
    BeskedfordelerService service) : ExternalBaseController
{
    [HttpPost]
    [AllowAnonymous] // KOMBIT authenticates with a client certificate instead of a KITOS user token.
    [Produces("application/xml")]
    public async Task<IActionResult> Receive()
    {
        if (!configuration.GetValue<bool>("Beskedfordeler:Enabled")) return NotFound();
        if (!Request.IsHttps || !certificates.IsValid(await HttpContext.Connection.GetClientCertificateAsync(HttpContext.RequestAborted)))
            return StatusCode(403);
        var type = Request.ContentType?.Split(';')[0].Trim();
        var status = string.Equals(type, "application/xml", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(type, "text/xml", StringComparison.OrdinalIgnoreCase)
            ? await service.Receive(Request.Body, HttpContext.RequestAborted)
            : BeskedfordelerStatus.CannotReceive;
        return Content(BeskedfordelerResponse.Serialize(status), "application/xml", Encoding.UTF8);
    }
}
