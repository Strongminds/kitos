﻿using Core.DomainModel;
using Core.DomainModel.ItSystemUsage;

namespace Core.DomainServices
{
    public interface IItSystemUsageService
    {
        ItSystemUsage Add(ItSystemUsage ItSystemUsage, User objectOwner);
        void Delete(int id);
        bool CanAddDataWorkerRelation(int usageId, int organizationId);
    }
}