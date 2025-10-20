using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Helpers;
using Core.ApplicationServices.Model.GDPR.Write;
using Core.ApplicationServices.Model.SystemUsage.Write;
using Core.DomainModel;
using Core.DomainModel.GDPR;
using Core.DomainModel.ItSystemUsage;

namespace Core.ApplicationServices.Mapping.Authorization
{
    public class SupplierAssociatedFieldKeyMapper: ISupplierAssociatedFieldKeyMapper
    {
        private readonly Dictionary<string, string> _dataProcessingParameterToSupplierFieldMap = new()
        {
            { nameof(UpdatedDataProcessingRegistrationOversightDataParameters.IsOversightCompleted), ObjectHelper.GetPropertyPath<DataProcessingRegistration>(x => x.IsOversightCompleted) },
            { nameof(UpdatedDataProcessingRegistrationOversightDateParameters.CompletedAt), ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightDate) },
            { nameof(UpdatedDataProcessingRegistrationOversightDateParameters.Remark), ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightRemark)},
            { nameof(UpdatedDataProcessingRegistrationOversightDateParameters.OversightReportLink), ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLink) },
            { nameof(UpdatedDataProcessingRegistrationOversightDateParameters.OversightReportLinkName), ObjectHelper.GetPropertyPath<DataProcessingRegistrationOversightDate>(x => x.OversightReportLinkName) },
        };
        private readonly Dictionary<string, string> _usageParameterToSupplierFieldMap = new()
        {
            { nameof(UpdatedSystemUsageGeneralProperties.ContainsAITechnology), ObjectHelper.GetPropertyPath<ItSystemUsage>(x => x.ContainsAITechnology) },
        };

        public IEnumerable<string> MapParameterKeysToDomainKeys(IEnumerable<string> properties, IEntity entity)
        {
            if (entity is DataProcessingRegistration)
                return MapDprRelatedKeys(properties).ToList();
            if (entity is ItSystemUsage)
                return MapUsageRelatedKeys(properties).ToList();

            return new List<string>();
        }

        private IEnumerable<string> MapDprRelatedKeys(IEnumerable<string> properties)
        {
            foreach (var key in properties)
            {
                if (_dataProcessingParameterToSupplierFieldMap.TryGetValue(key, out var mappedValue))
                {
                    yield return mappedValue;
                }

            }
        }
        private IEnumerable<string> MapUsageRelatedKeys(IEnumerable<string> properties)
        {
            foreach (var key in properties)
            {
                if (_usageParameterToSupplierFieldMap.TryGetValue(key, out var mappedValue))
                {
                    yield return mappedValue;
                }

            }
        }
    }
}