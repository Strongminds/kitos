﻿using Core.DomainModel.Events;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainServices;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.API.V1
{
    [InternalApi]
    public class ItSystemUsageRightsController : GenericRightsController<ItSystemUsage, ItSystemRight, ItSystemRole>
    {
        public ItSystemUsageRightsController(
            IGenericRepository<ItSystemRight> rightRepository, 
            IGenericRepository<ItSystemUsage> objectRepository,
            IDomainEvents domainEvents)
            : base(rightRepository, objectRepository, domainEvents)
        { }
    }
}
