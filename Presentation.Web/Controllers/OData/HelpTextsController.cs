﻿using Core.DomainServices;
using Core.DomainModel;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.OData
{
    [PublicApi]
    public class HelpTextsController : BaseEntityController<HelpText>
    {
        public HelpTextsController(IGenericRepository<HelpText> repository)
            : base(repository)
        {
        }
    }
}