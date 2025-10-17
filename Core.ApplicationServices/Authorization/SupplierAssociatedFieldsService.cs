using Core.ApplicationServices.Model;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.DomainModel;
using Core.DomainModel.GDPR;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Helpers;
using Core.DomainServices.Suppliers;

namespace Core.ApplicationServices.Authorization;

public class SupplierAssociatedFieldsService : ISupplierAssociatedFieldsService
{
    private readonly Dictionary<string, string> _dataProcessingParameterToSupplierFieldMap = new()
    {
        { nameof(UpdatedDataProcessingRegistrationOversightDataParameters.IsOversightCompleted), ObjectHelper.GetPropertyPath<DataProcessingRegistration>(x => x.IsOversightCompleted) },
        { nameof(DataProcessingRegistrationOversightDate.OversightDate), ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightDate) },
        { nameof(DataProcessingRegistrationOversightDate.OversightRemark), ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightRemark)},
        { nameof(DataProcessingRegistrationOversightDate.OversightReportLink), ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLink) },
    };

    private readonly ISupplierFieldDomainService _supplierFieldDomainService;

    public SupplierAssociatedFieldsService(ISupplierFieldDomainService supplierFieldDomainService)
    {
        _supplierFieldDomainService = supplierFieldDomainService;
    }

    public bool HasAnySupplierChanges(ISupplierAssociatedEntityUpdateParameters parameters, IEntity entity)
    {
        return parameters switch
        {
            DataProcessingRegistrationModificationParameters dprParameters => HasDprSupplierChanges(dprParameters, entity),
            UpdatedDataProcessingRegistrationOversightDateParameters oversightDateParameters =>
                HasOversightDateSupplierChanges(oversightDateParameters),
            _ => false
        };
    }

    public bool HasOnlySupplierChanges(ISupplierAssociatedEntityUpdateParameters parameters, IEntity entity)
    {
        return parameters switch
        {
            DataProcessingRegistrationModificationParameters dprParameters => HasNonDprSupplierChanges(
                dprParameters, entity),
            UpdatedDataProcessingRegistrationOversightDateParameters oversightDateParameters =>
                HasNonOversightDateSupplierChanges(oversightDateParameters),
            _ => false
        };
    }

    public bool RequestsDeleteToEntity<TEntity>(TEntity entity)
    {
        return entity switch
        {
            DataProcessingRegistrationOversightDate => true,
            _ => false
        };
    }

    public bool RequestsChangesToSupplierAssociatedFieldsInEnumerable(IEnumerable<ISupplierAssociatedEntityUpdateParameters> parametersEnumerable, IEntity entity)
    {
        var results = parametersEnumerable.Select(x => HasAnySupplierChanges(x, entity));
        return results.Any(r => r);
    }

    private bool HasDprSupplierChanges(DataProcessingRegistrationModificationParameters dprParams, IEntity entity)
    {
        if (entity is not DataProcessingRegistration dpr)
            return false;

        var oversight = dprParams.Oversight;
        if (oversight.IsNone)
            return false;

        var changedProperties = dprParams.GetChangedPropertyKeys(dpr);
        
        var res = _supplierFieldDomainService.AnySupplierFieldChanges(MapParameterKeysToDomainKeys(changedProperties));
        return res;
    }

    public IEnumerable<string> MapParameterKeysToDomainKeys(IEnumerable<string> properties)
    {
        foreach (var key in properties)
        {
            if (_dataProcessingParameterToSupplierFieldMap.TryGetValue(key, out var mappedValue))
            {
                yield return mappedValue;
            }
        }
    }

    private bool HasOversightDateSupplierChanges(UpdatedDataProcessingRegistrationOversightDateParameters parameters)
    {
        return _supplierFieldDomainService.AnySupplierFieldChanges(
            MapParameterKeysToDomainKeys(parameters.GetChangedPropertyKeys()));
    }

    public bool IsFieldSupplierControlled(string key)
    {
        return _supplierFieldDomainService.IsSupplierControlled(key);
    }

    private static bool HasNonOversightDateSupplierChanges(UpdatedDataProcessingRegistrationOversightDateParameters parameters)
    {
        return false;
    }

    private bool HasNonDprSupplierChanges(DataProcessingRegistrationModificationParameters dprParams, IEntity entity)
    {
        if(entity is not DataProcessingRegistration dpr)
            return false;

        var changedProperties = dprParams.GetChangedPropertyKeys(dpr);

        return _supplierFieldDomainService.OnlySupplierFieldChanges(MapParameterKeysToDomainKeys(changedProperties).ToList());
    }
}

