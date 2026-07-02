using System;
using System.Linq;
using System.Collections.Generic;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Response.ItContract;
using Core.ApplicationServices.OptionTypes;
using Microsoft.AspNetCore.Mvc;
using Core.DomainModel.ItContract;

namespace Presentation.Web.Controllers.API.V2.Internal.ItContracts
{
    [InternalApi]
    [Route("api/v2/internal/it-contracts/grid-roles")]
    public class GridLocalItContractRolesV2Controller : InternalApiV2Controller
    {
        private readonly IOptionsApplicationService<ItContractRight, ItContractRole> _optionService;

        public GridLocalItContractRolesV2Controller(IOptionsApplicationService<ItContractRight, ItContractRole> optionService)
        {
            _optionService = optionService;
        }

        [HttpGet]
        [Route("{organizationUuid}")]
        [ProducesResponseType(typeof(IEnumerable<LocalItContractRolesResponseDTO>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public IActionResult GetByOrganizationUuid(Guid organizationUuid)
        {
            return _optionService.GetOptionTypes(organizationUuid)
                .Select(options => options.Select(x => x.Option))
                .Select(roles => roles
                    .Select(role => new LocalItContractRolesResponseDTO(role.Id, role.Uuid, role.Name))
                    .ToList())
                .Match(Ok, FromOperationError);
        }
    }
}

