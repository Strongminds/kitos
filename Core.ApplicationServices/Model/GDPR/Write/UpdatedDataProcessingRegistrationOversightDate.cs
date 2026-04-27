using System;

namespace Core.ApplicationServices.Model.GDPR.Write
{
    public class UpdatedDataProcessingRegistrationOversightDate
    {
        public DateTime CompletedAt { get; set; }
        public required string Remark { get; set; }
        public required string OversightReportLink { get; set; }
        public required string OversightReportLinkName { get; set; }
    }
}