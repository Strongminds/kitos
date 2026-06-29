using Core.ApplicationServices.Model.SystemUsage;
using Core.ApplicationServices.SystemUsage;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Controllers.API.V2.External.Generic;
using Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages
{
    //TODO: Add get/delete endpoints
    [Route("api/v2/it-system-archives")]
    public class ItSystemArchiveV2Controller(
        IItSystemArchiveService archivedItSystemService,
        IItSystemArchiveResponseMapper responseMapper,
        IResourcePermissionsResponseMapper permissionsResponseMapper) : ExternalBaseController
    {
        /// <summary>
        /// Returns a specific IT-System archive by its UUID
        /// </summary>
        /// <param name="archiveUuid">UUID of the archive entity</param>
        /// <returns>The archive entity if found and user has read access</returns>
        [HttpGet]
        [Route("{archiveUuid:guid}")]
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
        [Route("{archiveUuid:guid}")]
        public IActionResult Delete([NonEmptyGuid] System.Guid archiveUuid)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
        
            return archivedItSystemService
                .Delete(archiveUuid)
                .Match(_ => NoContent(), FromOperationError);
        }

        /// <summary>
        /// Returns the permissions of the authenticated client in the context of a specific IT-System archive (a specific IT-System in a specific Organization)
        /// </summary>
        /// <param name="archiveUuid">UUID of the archive entity</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{archiveUuid}/permissions")]
        public IActionResult GetItSystemArchivePermissions([NonEmptyGuid] Guid archiveUuid)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return archivedItSystemService
                .GetPermissions(archiveUuid)
                .Select(permissionsResponseMapper.Map)
                .Match(Ok, FromOperationError);
        }

        /// <summary>
        /// Returns the permissions of the authenticated client for the IT-System archive resources collection in the context of an organization (IT-System archive permissions in a specific Organization)
        /// </summary>
        /// <param name="organizationUuid">UUID of the organization</param>
        /// <returns></returns>
        [HttpGet]
        [Route("permissions")]
        public IActionResult GetItSystemArchiveCollectionPermissions([Required][NonEmptyGuid] Guid organizationUuid)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return archivedItSystemService
                .GetCollectionPermissions(organizationUuid)
                .Select(permissionsResponseMapper.Map)
                .Match(Ok, FromOperationError);
        }
    }
}

