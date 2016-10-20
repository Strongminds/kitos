﻿using Core.ApplicationServices;
using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainModel.LocalOptions;
using Core.DomainServices;

namespace Presentation.Web.Controllers.OData.LocalOptionControllers
{
    public class LocalFrequencyTypesController : LocalOptionBaseController<LocalFrequencyType, DataRowUsage, FrequencyType>
    {
        public LocalFrequencyTypesController(IGenericRepository<LocalFrequencyType> repository, IAuthenticationService authService, IGenericRepository<FrequencyType> optionsRepository)
            : base(repository, authService, optionsRepository)
        {
        }
    }
}
