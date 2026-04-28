using Core.DomainModel.ItSystem.DataTypes;
using System;

namespace Core.DomainModel.ItSystemUsage
{
    public class ItSystemUsageCriticalityInfo
    {
        public DataOptions? IsBusinessCritical { get; private set; }
        public DataOptions? IsSociallyCritical { get; private set; }
        public DateTime? LastChanged { get; set; }

        public void UpdateIsBusinessCritical(DataOptions? value) {
            IsBusinessCritical = value;
            LastChanged = DateTime.UtcNow;
        }

        public void UpdateIsSociallyCritical(DataOptions? value)
        {
            IsSociallyCritical = value;
            LastChanged = DateTime.UtcNow;
        }
    }
}
