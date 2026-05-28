using Core.Abstractions.Extensions;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainServices.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Core.DomainServices.Mapping
{
    public static class ReadModelMappingHelpers
    {
        public static string MapItSystemName(this ItSystemUsage systemUsage)
        {
            return systemUsage.ItSystem.FromNullable().Select(system => system.MapItSystemName()).GetValueOrDefault() ?? string.Empty;
        }

        public static string MapItSystemName(this ItSystem system)
        {
            return $"{system.Name}{(system.Disabled ? " (Ikke tilgængeligt)" : "")}";
        }

        public static string MapDataProcessingRegistrationSystemUsages(this ICollection<ItSystemUsage> usages)
        {
            return usages.Select(x => x.CheckSystemValidity().Result == true ? "(Aktivt)" : "(Ikke aktivt)").ToStringWithDelimiter();
        }
    }
}
