using Core.ApplicationServices.Model.SystemUsage;
using Presentation.Web.Models.API.V2.Request.SystemUsage;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping
{
    public interface IItSystemArchiveWriteRequestMapper
    {
        ArchiveItSystemUsageParameters FromRequest(CreateItSystemUsageArchiveRequestDTO request);
    }
}
