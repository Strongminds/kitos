using Core.ApplicationServices.Model.SystemUsage;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Request.SystemUsage;
using System;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages
{
    [Route("api/v2/it-system-usages/archives")]
    public class ItSystemUsageArchivesV2Controller(
        IItSystemUsageArchiveService archivedItSystemUsageService,
        IItSystemArchiveResponseMapper responseMapper,
        IItSystemArchiveWriteRequestMapper writeRequestMapper) : ExternalBaseController
    {
        [HttpPost]
        [Route("{systemUsageUuid}")]
        public IActionResult ArchiveItsystemUsage(
            [NonEmptyGuid] Guid systemUsageUuid,
            [FromBody] CreateItSystemUsageArchiveRequestDTO request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var parameters = writeRequestMapper.FromRequest(request);
            return archivedItSystemUsageService.Create(systemUsageUuid, parameters).Match(
                archive => Ok(responseMapper.ToResponseDTO(archive)),
                FromOperationError);
        }
    }
}
