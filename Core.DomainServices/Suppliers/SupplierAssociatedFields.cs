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
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistration>(x => x.IsOversightCompleted), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightDate), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightRemark), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLink), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightOptionId), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.ContainsAITechnology), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.SystemUsageCriticalityLevel), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.preriskAssessment), ControlState = SupplierAssociatedFieldControlState.SUPPLIER },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLinkName), ControlState = SupplierAssociatedFieldControlState.SHARED },
                new SupplierAssociatedFieldConfiguration { FieldKey = ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.riskAssessment), ControlState = SupplierAssociatedFieldControlState.SHARED },
            };
    }
}
