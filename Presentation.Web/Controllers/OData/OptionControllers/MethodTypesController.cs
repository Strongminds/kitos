﻿using Core.ApplicationServices;
using Core.DomainModel.ItSystem;
using Core.DomainServices;

namespace Presentation.Web.Controllers.OData.OptionControllers
{
    public class MethodTypesController : BaseRoleController<MethodType, ItInterface>
    {
        public MethodTypesController(IGenericRepository<MethodType> repository, IAuthenticationService authService)
            : base(repository, authService)
        {
        }
    }
}