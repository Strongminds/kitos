using System;
using System.Web.Http;
using Core.ApplicationServices.System.Write;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Request.System.Regular;

namespace Presentation.Web.Controllers.API.V2.External.ItSystems;

[RoutePrefix("api/v2/it-systems/{systemUuid}/dbs")]
public class ItSystemDBSV2Controller : ExternalBaseController
{
    private readonly IItSystemWriteService _writeService;

    public ItSystemDBSV2Controller(IItSystemWriteService writeService)
    {
        _writeService = writeService;
    }

    [Route]
    public IHttpActionResult PatchDbsProperties([NonEmptyGuid] [FromUri] Guid systemUuid, UpdateDBSPropertiesRequestDTO request)
    {
        return NotFound();
    }
}