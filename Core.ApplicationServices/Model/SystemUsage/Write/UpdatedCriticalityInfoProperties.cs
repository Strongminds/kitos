using Core.ApplicationServices.Model.Shared;
using Core.DomainModel.ItSystem.DataTypes;

namespace Core.ApplicationServices.Model.SystemUsage.Write
{
    public class UpdatedCriticalityInfoProperties
    {
        public OptionalValueChange<DataOptions?> BusinessCritical { get; set; } = OptionalValueChange<DataOptions?>.None;
        public OptionalValueChange<DataOptions?> IsSociallyCritical { get; set; } = OptionalValueChange<DataOptions?>.None;
    }
}
