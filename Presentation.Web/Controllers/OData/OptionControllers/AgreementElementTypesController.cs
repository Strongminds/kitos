﻿using Core.DomainModel.ItContract;
using Core.DomainServices;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.OData.OptionControllers
{
    [InternalApi]
    [MigratedToNewAuthorizationContext]
    public class AgreementElementTypesController : BaseOptionController<AgreementElementType, ItContract>
    {
        public AgreementElementTypesController(IGenericRepository<AgreementElementType> repository)
            : base(repository)
        {
        }
    }
}