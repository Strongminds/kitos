using Presentation.Web.Models.API.V2.Types.Shared;

namespace Presentation.Web.Models.API.V2.Response.SystemUsage
{
    public class CriticalitySectionResponseDTO
    {
        /// <summary>
        /// Whether this system is considered business critical.
        /// </summary>
        public YesNoDontKnowChoice? BusinessCritical { get; set; }
    }
}
