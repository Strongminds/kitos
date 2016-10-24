﻿using Core.ApplicationServices;
using Core.DomainModel.ItContract;
using Core.DomainServices;

namespace Presentation.Web.Controllers.OData.OptionControllers
{
    public class AgreementElementTypesController : BaseEntityController<AgreementElementType>
    {
        public AgreementElementTypesController(IGenericRepository<AgreementElementType> repository, IAuthenticationService authService)
            : base(repository, authService)
        {
        }
    }
}