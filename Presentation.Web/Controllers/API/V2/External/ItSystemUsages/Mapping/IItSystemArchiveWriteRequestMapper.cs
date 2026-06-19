using Core.ApplicationServices.Model.SystemUsage;
using Presentation.Web.Models.API.V2.Request.SystemUsage;

namespace Presentation.Web.Controllers.API.V2.External.ItSystemUsages.Mapping
{
    public interface IItSystemArchiveWriteRequestMapper
    {
        CreateItSystemArchiveParameters FromRequest(CreateItSystemUsageArchiveRequestDTO request);
    }
}
