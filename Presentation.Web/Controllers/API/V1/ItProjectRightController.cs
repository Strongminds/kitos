﻿using Core.DomainModel.Events;
using Core.DomainModel.ItProject;
using Core.DomainServices;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.API.V1
{
    [PublicApi]
    public class ItProjectRightController : GenericRightsController<ItProject, ItProjectRight, ItProjectRole>
    {
        public ItProjectRightController(
            IGenericRepository<ItProjectRight> rightRepository, 
            IGenericRepository<ItProject> objectRepository,
            IDomainEvents domainEvents)
            : base(rightRepository, objectRepository, domainEvents)
        {
        }
    }
}
