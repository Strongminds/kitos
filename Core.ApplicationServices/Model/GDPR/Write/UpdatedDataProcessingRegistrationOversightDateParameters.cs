using System;
using System.Collections.Generic;
using Core.ApplicationServices.Model.Shared;

namespace Core.ApplicationServices.Model.GDPR.Write
{
    public class UpdatedDataProcessingRegistrationOversightDateParameters: ISupplierAssociatedEntityUpdateParameters
    {
        public required OptionalValueChange<DateTime> CompletedAt { get; set; }
        public required OptionalValueChange<string> Remark { get; set; }
        public required OptionalValueChange<string> OversightReportLink { get; set; }
        public required OptionalValueChange<string> OversightReportLinkName { get; set; }

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
