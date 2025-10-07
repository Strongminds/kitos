using Presentation.Web.Models.API.V2.Types.Shared;
using System;

namespace Presentation.Web.Models.API.V2.Types.DataProcessing
{
    public class ModifyOversightDateDTO : IOversightDateDTO
    {
        /// <summary>
        /// Date of oversight completion
        /// </summary>
        public DateTime CompletedAt { get; set; }
        /// <summary>
        /// Optional remark related to the oversight
        /// </summary>
        public string Remark { get; set; }

        public SimpleLinkDTO OversightReportLink { get; set; }
    }
}