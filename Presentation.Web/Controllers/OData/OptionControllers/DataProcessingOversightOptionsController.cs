﻿using Core.DomainModel.GDPR;
using Core.DomainServices;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.OData.OptionControllers
{
    [InternalApi]
    public class DataProcessingOversightOptionsController : BaseOptionController<DataProcessingOversightOption, DataProcessingRegistration>
    {
        public DataProcessingOversightOptionsController(IGenericRepository<DataProcessingOversightOption> repository)
            : base(repository)
        {
        }
    }
}
