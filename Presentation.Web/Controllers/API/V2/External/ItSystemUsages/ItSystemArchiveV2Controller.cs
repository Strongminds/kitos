using Core.ApplicationServices.Model.SystemUsage;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages
{
    //TODO: Add get/delete endpoints
    [Route("api/v2/it-system-archives")]
    public class ItSystemArchiveV2Controller(
        IItSystemArchiveService archivedItSystemService,
        IItSystemArchiveResponseMapper responseMapper) : ExternalBaseController
    {
        /// <summary>
        /// Returns a specific IT-System archive by its UUID
        /// </summary>
        /// <param name="archiveUuid">UUID of the archive entity</param>
        /// <returns>The archive entity if found and user has read access</returns>
        [HttpGet]
        [Route("{archiveUuid}")]
        public IActionResult Get([NonEmptyGuid] System.Guid archiveUuid)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return archivedItSystemService
                .GetByUuid(archiveUuid)
                .Select(responseMapper.ToResponseDTO)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Deletes a specific IT-System archive by its UUID
        /// </summary>
        /// <param name="archiveUuid">UUID of the archive entity to delete</param>
        /// <returns>No content if successfully deleted</returns>
        [HttpDelete]
        [Route("{archiveUuid}")]
        public IActionResult Delete([NonEmptyGuid] System.Guid archiveUuid)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
        
            return archivedItSystemService
                .Delete(archiveUuid)
                .Match(_ => NoContent(), FromOperationError);
        }
    }
}

