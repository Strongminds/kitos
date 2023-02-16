﻿using Core.DomainModel.ItSystemUsage;
using Core.DomainServices;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.API.V1.OData.OptionControllers
{
    [InternalApi]
    public class RegisterTypesController : BaseOptionController<RegisterType, ItSystemUsage>
    {
        public RegisterTypesController(IGenericRepository<RegisterType> repository)
            : base(repository)
        {
        }
    }
}