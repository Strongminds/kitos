using Core.ApplicationServices.Model.SystemUsage;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages
{
    //TODO: Add get/delete endpoints
    [Route("api/v2/it-system-archives")]
    public class ItSystemArchiveV2Controller(
        IItSystemArchiveService archivedItSystemService,
        IItSystemArchiveResponseMapper responseMapper) : ExternalBaseController
    {
        
    }
}
