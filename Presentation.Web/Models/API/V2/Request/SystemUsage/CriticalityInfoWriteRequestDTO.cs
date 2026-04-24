using Presentation.Web.Models.API.V2.Types.Shared;

namespace Presentation.Web.Models.API.V2.Request.SystemUsage
{
    public class CriticalityInfoWriteRequestDTO
    {
        /// <summary>
        /// Whether the system is considered business critical.
        /// </summary>
        public YesNoDontKnowChoice? BusinessCritical { get; set; }
    }
}
