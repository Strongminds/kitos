using System.Collections.Generic;
using System.Net;
using System;
using System.Web.Http;
using Core.ApplicationServices.OptionTypes;
using Core.DomainModel.Organization;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Request.Generic.Queries;
using Presentation.Web.Models.API.V2.Response.Options;
using Swashbuckle.Swagger.Annotations;

namespace Presentation.Web.Controllers.API.V2.External.Organizations
{
    [RoutePrefix("api/v2/country-codes")]
    [AllowRightsHoldersAccess]
    public class OrganizationCountryCodesV2Controller
        : BaseRegularOptionTypeV2Controller<Organization, CountryCode>
    {
        public OrganizationCountryCodesV2Controller(IOptionsApplicationService<Organization, CountryCode> optionApplicationService) 
            : base(optionApplicationService)
        {
        }

        /// <summary>
        /// Returns country code types which are available for new registrations within the organization
        /// </summary>
        /// <param name="organizationUuid">organization context for the country code availability</param>
        /// <returns>A list of available country code specifics formatted as uuid and name pairs</returns>
        [HttpGet]
        [Route]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<RegularOptionResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult GetCountryCodes([NonEmptyGuid] Guid organizationUuid, [FromUri] UnboundedPaginationQuery pagination = null)
        {
            return GetAll(organizationUuid, pagination);
        }

        /// <summary>
        /// Returns requested country code
        /// </summary>
        /// <param name="countryCodeUuid">country code identifier</param>
        /// <param name="organizationUuid">organization context for the country code availability</param>
        /// <returns>A uuid and name pair with boolean to mark if the country code is available in the organization</returns>
        [HttpGet]
        [Route("{countryCodeUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(RegularOptionExtendedResponseDTO))]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        [SwaggerResponse(HttpStatusCode.Forbidden)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        public IHttpActionResult GetCountryCode([NonEmptyGuid] Guid countryCodeUuid, [NonEmptyGuid] Guid organizationUuid)
        {
            return GetSingle(countryCodeUuid, organizationUuid);
        }
    }
}