using Core.ApplicationServices.OptionTypes;
using Core.DomainModel.ItContract;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Response.Options;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Models.API.V2.Request.Generic.Queries;

namespace Presentation.Web.Controllers.API.V2.External.ItContracts
{
    [Route("api/v2/it-contract-price-regulation-types")]
    public class ItContractPriceRegulationTypeV2Controller : BaseRegularOptionTypeV2Controller<ItContract, PriceRegulationType>
    {
        public ItContractPriceRegulationTypeV2Controller(IOptionsApplicationService<ItContract, PriceRegulationType> optionService)
            : base(optionService)
        {
        }

        /// <summary>
        /// Returns It-Contract price regulation type options 
        /// </summary>
        /// <param name="organizationUuid">organization context for the price regulation types availability</param>
        /// <returns>A list of available It-Contract price regulation types</returns>
        [HttpGet]
        [Route("")]
        public IActionResult Get([NonEmptyGuid] Guid organizationUuid, [FromQuery] UnboundedPaginationQuery pagination = null)
        {
            return GetAll(organizationUuid, pagination);
        }

        /// <summary>
        /// Returns requested It-Contract price regulation type
        /// </summary>
        /// <param name="priceRegulationTypeUuid">price regulation type identifier</param>
        /// <param name="organizationUuid">organization context for the price regulation type availability</param>
        /// <returns>A uuid and name pair with boolean to mark if the price regulation type is available in the organization</returns>
        [HttpGet]
        [Route("{priceRegulationTypeUuid}")]
        public IActionResult Get([NonEmptyGuid] Guid priceRegulationTypeUuid, [NonEmptyGuid] Guid organizationUuid)
        {
            return GetSingle(priceRegulationTypeUuid, organizationUuid);
        }
    }
}


