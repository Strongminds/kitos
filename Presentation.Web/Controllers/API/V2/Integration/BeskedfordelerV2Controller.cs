using System;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.STS.Beskedfordeler;

using Microsoft.AspNetCore.Mvc;

namespace Presentation.Web.Controllers.API.V2.Integration
{
    [Route("api/v2/integration/beskedfordeler")]
    public sealed class BeskedfordelerV2Controller(
        BeskedfordelerService service) : IntegrationBaseController
    {
        [HttpPost]
        [Produces("application/xml")]
        public async Task<IActionResult> Receive()
        {
            var type = Request.ContentType?.Split(';')[0].Trim();
            var status = string.Equals(type, "application/xml", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(type, "text/xml", StringComparison.OrdinalIgnoreCase)
                ? await service.Receive(Request.Body, HttpContext.RequestAborted)
                : BeskedfordelerStatus.CannotReceive;
            return Content(BeskedfordelerResponse.Serialize(status), "application/xml", Encoding.UTF8);
        }
    }
}
