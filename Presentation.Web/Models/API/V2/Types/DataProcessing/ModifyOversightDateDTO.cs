using Presentation.Web.Models.API.V2.Types.Shared;
using System;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Models.API.V2.Types.DataProcessing
{
    public class ModifyOversightDateDTO : IOversightDateDTO
    {
        /// <summary>
        /// Date of oversight completion
        /// </summary>
        [SupplierField]
        public DateTime CompletedAt { get; set; }
        /// <summary>
        /// Optional remark related to the oversight
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// (Supplier Field): OversightReportLink.Url
        /// (Shared Supplier Field): OversightReportLink.Name
        /// </summary>
        public SimpleLinkDTO OversightReportLink { get; set; }
    }
}