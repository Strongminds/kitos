﻿using Core.DomainModel.ItSystem;
using Core.DomainModel.ItSystemUsage;
using Core.DomainServices;
using Presentation.Web.Models;

namespace Presentation.Web.Controllers.API
{
    public class SensitiveDataTypeController : GenericOptionApiController<SensitiveDataType, ItSystemUsage, OptionDTO>
    {
        public SensitiveDataTypeController(IGenericRepository<SensitiveDataType> repository)
            : base(repository)
        {
        }
    }
}
