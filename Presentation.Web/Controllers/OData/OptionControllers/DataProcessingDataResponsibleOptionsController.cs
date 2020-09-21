﻿using Core.DomainModel.GDPR;
using Core.DomainServices;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.OData.OptionControllers
{
    [InternalApi]
    public class DataProcessingDataResponsibleOptionsController : BaseOptionController<DataProcessingDataResponsibleOption, DataProcessingAgreement>
    {
        public DataProcessingDataResponsibleOptionsController(IGenericRepository<DataProcessingDataResponsibleOption> repository)
            : base(repository)
        {
        }
    }
}
