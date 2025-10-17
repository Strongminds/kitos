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

            if(CompletedAt.HasChange)
                changedProperties.Add(nameof(CompletedAt));
            if(Remark.HasChange)
                changedProperties.Add(nameof(Remark));
            if(OversightReportLink.HasChange)
                changedProperties.Add(nameof(OversightReportLink));
            if(OversightReportLinkName.HasChange)
                changedProperties.Add(nameof(OversightReportLinkName));
            return changedProperties;
        }
    }
}
