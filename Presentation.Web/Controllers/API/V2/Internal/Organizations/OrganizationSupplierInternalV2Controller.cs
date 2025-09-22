using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Response.Organizations;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Net;
using System.Web.Http;

namespace Presentation.Web.Controllers.API.V2.Internal.Organizations
{
    /// <summary>
    /// Internal API for the organizations in KITOS
    /// </summary>
    /*[RoutePrefix("api/v2/internal/organizations")]
    public class OrganizationSupplierInternalV2Controller : InternalApiV2Controller
    {


        [Route("{organizationUuid}/suppliers")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(UIRootConfigResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetSuppliers([NonEmptyGuid] Guid organizationUuid)
        {
            return _organizationService.GetUIRootConfig(organizationUuid)
                .Select(_organizationResponseMapper.ToUIRootConfigDTO)
                .Match(Ok, FromOperationError);
        }
    }*/
}