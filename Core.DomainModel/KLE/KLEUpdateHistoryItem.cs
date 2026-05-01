using System;

namespace Core.DomainModel.KLE
{
    public class KLEUpdateHistoryItem: Entity
    {
        public DateTime Version { get; set; }

        // To keep EF and proxy generation happy
        protected KLEUpdateHistoryItem() {}

        public KLEUpdateHistoryItem(DateTime version)
        {
            Version = version;
        }
    }
}