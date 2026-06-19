using Core.ApplicationServices.Model.SystemUsage;
using Presentation.Web.Models.API.V2.Request.SystemUsage;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping
{
    public class ItSystemArchiveWriteRequestMapper : IItSystemArchiveWriteRequestMapper
    {
        public CreateItSystemArchiveParameters FromRequest(CreateItSystemUsageArchiveRequestDTO request)
        {
            return new CreateItSystemArchiveParameters
            {
                ArchivingDate = request.ArchivingDate,
                ReferenceName = request.ReferenceName,
                Note = request.Note
            };
        }
    }
}
