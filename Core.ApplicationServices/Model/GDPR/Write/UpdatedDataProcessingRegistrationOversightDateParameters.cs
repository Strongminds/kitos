using System;
using Core.ApplicationServices.Model.Shared;

namespace Core.ApplicationServices.Model.GDPR.Write
{
    public class UpdatedDataProcessingRegistrationOversightDateParameters: ISupplierAssociatedEntityUpdateParameters
    {
        public OptionalValueChange<DateTime> CompletedAt { get; set; }
        public OptionalValueChange<string> Remark { get; set; }
        public OptionalValueChange<string> OversightReportLink { get; set; }
        public OptionalValueChange<string> OversightReportLinkName { get; set; }
    }
}
