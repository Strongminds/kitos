using Core.ApplicationServices.Model.SystemUsage;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Infrastructure.Attributes;
using System;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages
{
    [Route("api/v2/it-system-usages/archives")]
    public class ItSystemUsageArchivesV2Controller(IItSystemUsageArchiveService archivedItSystemUsageService) : ExternalBaseController
    {
        [HttpPost]
        [Route("{systemUsageUuid}")]
        public IActionResult ArchiveItsystemUsage([NonEmptyGuid] Guid systemUsageUuid)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            return archivedItSystemUsageService.Create(systemUsageUuid).Match(Ok, FromOperationError);
        }
    }
}
