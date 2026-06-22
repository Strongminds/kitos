using Core.ApplicationServices.Model.SystemUsage;
using System.Collections.Generic;
using System.Linq;
using Presentation.Web.Models.API.V2.Request.SystemUsage;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping
{
    public class ItSystemArchiveWriteRequestMapper : IItSystemArchiveWriteRequestMapper
    {
        public ArchiveItSystemUsageParameters FromRequest(CreateItSystemUsageArchiveRequestDTO request)
        {
            return new ArchiveItSystemUsageParameters
            {
                ArchivingDate = request.ArchivingDate,
                ReferenceName = request.ReferenceName,
                Note = request.Note,
                ArchiveReferences = request.ArchiveReferences?.Select(reference => new ArchiveReferenceProperties
                {
                    Label = reference.Label,
                    Url = reference.Url
                }) ?? new List<ArchiveReferenceProperties>()
            };
        }
    }
}
