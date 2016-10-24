﻿using Core.ApplicationServices;
using Core.DomainModel.ItProject;
using Core.DomainServices;

namespace Presentation.Web.Controllers.OData.OptionControllers
{
    public class ItProjectTypesController : BaseEntityController<ItProjectType>
    {
        public ItProjectTypesController(IGenericRepository<ItProjectType> repository, IAuthenticationService authService)
            : base(repository, authService)
        {
        }
    }
}