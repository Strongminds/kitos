using System;
using System.Collections.Generic;
using Core.ApplicationServices.Model.Shared;

namespace Core.ApplicationServices.Model.GDPR.Write
{
    public class UpdatedDataProcessingRegistrationOversightDateParameters: ISupplierAssociatedEntityUpdateParameters
    {
        public OptionalValueChange<DateTime> CompletedAt { get; set; }
        public OptionalValueChange<string> Remark { get; set; }
        public OptionalValueChange<string> OversightReportLink { get; set; }
        public OptionalValueChange<string> OversightReportLinkName { get; set; }

        public IEnumerable<string> GetChangedPropertyKeys()
        {
            var changedProperties = new List<string>();

            if(CompletedAt != null && CompletedAt.HasChange)
                changedProperties.Add(nameof(CompletedAt));
            if(Remark != null && Remark.HasChange)
                changedProperties.Add(nameof(Remark));
            if(OversightReportLink != null && OversightReportLink.HasChange)
                changedProperties.Add(nameof(OversightReportLink));
            if(OversightReportLinkName != null && OversightReportLinkName.HasChange)
                changedProperties.Add(nameof(OversightReportLinkName));
            return changedProperties;
        }
    }
}
