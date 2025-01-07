using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.OptionTypes;
using Core.DomainModel.ItContract;

namespace Core.ApplicationServices.Contract.ReadModels
{
    public class GridLocalItContractRolesService : IGridLocalItContractRolesService
    {
        private readonly IOptionsApplicationService<ItContractRight, ItContractRole> _optionService;

        public GridLocalItContractRolesService(IOptionsApplicationService<ItContractRight, ItContractRole> optionService)
        {
            _optionService = optionService;
        }

        public Result<IEnumerable<ItContractRole>, OperationError> GetOverviewRoles(Guid organizationUuid)
        {
            return _optionService.GetOptionTypes(organizationUuid).Select(options => options.Select(x => x.Option));

        }
    }
}
