using Core.Abstractions.Helpers;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.SupplierAssociatedFields;
using System.Collections.Generic;

namespace Core.DomainServices.Suppliers
{
    public static class SupplierAssociatedFields
    {
        public static ISet<SupplierAssociatedFieldConfiguration> DefaultConfiguration = new HashSet<SupplierAssociatedFieldConfiguration>
            {
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistration>(x => x.IsOversightCompleted), ControlState = SupplierAssociatedFieldControlState.Supplier },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightDate), ControlState = SupplierAssociatedFieldControlState.Supplier },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightRemark), ControlState = SupplierAssociatedFieldControlState.Supplier },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLink), ControlState = SupplierAssociatedFieldControlState.Supplier },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightOptionId), ControlState = SupplierAssociatedFieldControlState.Supplier },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.ContainsAITechnology), ControlState = SupplierAssociatedFieldControlState.Supplier },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.SystemUsageCriticalityLevel), ControlState = SupplierAssociatedFieldControlState.Supplier },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.preriskAssessment), ControlState = SupplierAssociatedFieldControlState.Supplier },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLinkName), ControlState = SupplierAssociatedFieldControlState.Shared },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.riskAssessment), ControlState = SupplierAssociatedFieldControlState.Shared },
            };
    }
}
