using Core.ApplicationServices.Model;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.DomainModel;
using Core.DomainModel.GDPR;
using System.Collections.Generic;
using System.Linq;
using Core.ApplicationServices.Mapping.Authorization;
using Core.ApplicationServices.Model.SystemUsage.Write;
using Core.DomainServices.Suppliers;

namespace Core.ApplicationServices.Authorization;

public class SupplierAssociatedFieldsService : ISupplierAssociatedFieldsService
{
    private readonly ISupplierFieldDomainService _supplierFieldDomainService;
    private readonly ISupplierAssociatedFieldKeyMapper _mapper;

    public SupplierAssociatedFieldsService(ISupplierFieldDomainService supplierFieldDomainService, ISupplierAssociatedFieldKeyMapper mapper)
    {
        _supplierFieldDomainService = supplierFieldDomainService;
        _mapper = mapper;
    }

    public bool HasAnySupplierChanges(ISupplierAssociatedEntityUpdateParameters parameters, IEntity entity)
    {
        return parameters switch
        {
            DataProcessingRegistrationModificationParameters dprParameters => HasDprSupplierChanges(dprParameters, entity),
            UpdatedDataProcessingRegistrationOversightDateParameters oversightDateParameters =>
                HasOversightDateSupplierChanges(oversightDateParameters, entity),
            SystemUsageUpdateParameters usageParameters => HasUsageSupplierChanges(usageParameters, entity),
            _ => false
        };
    }

    public bool HasOnlySupplierChanges(ISupplierAssociatedEntityUpdateParameters parameters, IEntity entity)
    {
        return parameters switch
        {
            DataProcessingRegistrationModificationParameters dprParameters => HasOnlyDprSupplierChanges(
                dprParameters, entity),
            UpdatedDataProcessingRegistrationOversightDateParameters oversightDateParameters =>
                HasOnlyOversightDateSupplierChanges(oversightDateParameters, entity),
            SystemUsageUpdateParameters usageParameters => HasOnlyUsageSupplierChanges(usageParameters, entity),
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

    public bool HasAnySupplierChangesList(IEnumerable<ISupplierAssociatedEntityUpdateParameters> parametersEnumerable, IEntity entity)
    {
        var results = parametersEnumerable.Select(x => HasAnySupplierChanges(x, entity));
        return results.Any(r => r);
    }

    private bool HasDprSupplierChanges(DataProcessingRegistrationModificationParameters dprParams, IEntity entity)
    {
        if (entity is not DataProcessingRegistration dpr)
            return false;
        
        var changedProperties = dprParams.GetChangedPropertyKeys(dpr);
        
        return _supplierFieldDomainService.ContainsAnySupplierControlledFields(_mapper.MapParameterKeysToDomainKeys(changedProperties, entity));
    }

    private bool HasOversightDateSupplierChanges(UpdatedDataProcessingRegistrationOversightDateParameters parameters, IEntity entity)
    {
        var changedProperties = parameters.GetChangedPropertyKeys();
        var keys = _mapper.MapParameterKeysToDomainKeys(changedProperties, entity);
        return _supplierFieldDomainService.ContainsAnySupplierControlledFields(keys);
    }

    private bool HasUsageSupplierChanges(SystemUsageUpdateParameters parameters, IEntity entity)
    {

        var changedProperties = parameters.GetChangedPropertyKeys();
        var keys = _mapper.MapParameterKeysToDomainKeys(changedProperties, entity);

        return _supplierFieldDomainService.ContainsAnySupplierControlledFields(keys);
    }

    public bool IsFieldSupplierControlled(string key)
    {
        return _supplierFieldDomainService.IsSupplierControlled(key);
    }

    private bool HasOnlyOversightDateSupplierChanges(UpdatedDataProcessingRegistrationOversightDateParameters parameters, IEntity entity)
    {
        var changedProperties = parameters.GetChangedPropertyKeys();
        return _supplierFieldDomainService.ContainsOnlySupplierControlledField(_mapper.MapParameterKeysToDomainKeys(changedProperties, entity));
    }

    private bool HasOnlyDprSupplierChanges(DataProcessingRegistrationModificationParameters dprParams, IEntity entity)
    {
        if(entity is not DataProcessingRegistration dpr)
            return false;

        var changedProperties = dprParams.GetChangedPropertyKeys(dpr);

        return _supplierFieldDomainService.ContainsOnlySupplierControlledField(_mapper.MapParameterKeysToDomainKeys(changedProperties, entity));
    }

    private bool HasOnlyUsageSupplierChanges(SystemUsageUpdateParameters parameters, IEntity entity)
    {
        var changedProperties = parameters.GetChangedPropertyKeys();
        var keys = _mapper.MapParameterKeysToDomainKeys(changedProperties, entity);
        return _supplierFieldDomainService.ContainsOnlySupplierControlledField(keys);
    }
}

