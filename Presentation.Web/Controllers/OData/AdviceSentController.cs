﻿using Core.DomainModel.AdviceSent;
using Core.DomainServices;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.OData
{
    [InternalApi]
    public class AdviceSentController : BaseEntityController<AdviceSent>
    {
        public AdviceSentController(IGenericRepository<AdviceSent> repository): 
        base(repository){ }
    }
}