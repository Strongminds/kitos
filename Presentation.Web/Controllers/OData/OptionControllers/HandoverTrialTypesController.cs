﻿using System.Web.Http.Description;
using Core.ApplicationServices;
using Core.DomainModel.ItContract;
using Core.DomainServices;

namespace Presentation.Web.Controllers.OData.OptionControllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HandoverTrialTypesController : BaseOptionController<HandoverTrialType, HandoverTrial>
    {
        public HandoverTrialTypesController(IGenericRepository<HandoverTrialType> repository, IAuthenticationService authService)
            : base(repository, authService)
        {
        }
    }
}