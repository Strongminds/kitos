using Core.DomainModel.Archive;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Models.API.V2.Response.SystemUsage;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping
{
    public class ItSystemArchiveResponseMapper : IItSystemArchiveResponseMapper
    {
        public ItSystemArchiveResponseDTO ToResponseDTO(ItSystemArchive archive)
        {
            return new ItSystemArchiveResponseDTO
            {
                Uuid = archive.Uuid,
                ArchivingDate = archive.ArchivingDate,
                ReferenceName = archive.ReferenceName,
                Note = archive.Note,
                Organization = archive.Organization?.MapShallowOrganizationResponseDTO()
            };
        }
    }
}
