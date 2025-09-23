using Core.ApplicationServices.Organizations.Write;
using Core.DomainModel.Organization;
using Presentation.Web.Controllers.API.V1.Mapping;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Response.Organization;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Presentation.Web.Controllers.API.V2.Internal.Organizations
{
    /// <summary>
    /// Internal API for the organizations in KITOS
    /// </summary>
    [RoutePrefix("api/v2/internal/organizations")]
    public class OrganizationSupplierInternalV2Controller : InternalApiV2Controller
    {
        private readonly IOrganizationSupplierWriteService _organizationSupplierService;

        public OrganizationSupplierInternalV2Controller(IOrganizationSupplierWriteService organizationSupplierService)
        {
            _organizationSupplierService = organizationSupplierService;
        }

        [Route("{organizationUuid}/suppliers")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<ShallowOrganizationResponseDTO>))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult GetSuppliers([NonEmptyGuid] Guid organizationUuid)
        {
            return _organizationSupplierService.GetSuppliersForOrganization(organizationUuid)
                .Select(MapToResponse)
                .Match(Ok, FromOperationError);
        }

        [HttpPost]
        [Route("{organizationUuid}/suppliers/{supplierUuid}")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ShallowOrganizationResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult AddSupplier([NonEmptyGuid] Guid organizationUuid, [NonEmptyGuid] Guid supplierUuid)
        {
            return _organizationSupplierService.AddSupplierToOrganization(organizationUuid, supplierUuid)
                .Select(MapSingleToResponse)
                .Match(Ok, FromOperationError);
        }

        [HttpDelete]
        [Route("{organizationUuid}/suppliers/{supplierUuid}")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult DeleteSupplier([NonEmptyGuid] Guid organizationUuid, [NonEmptyGuid] Guid supplierUuid)
        {
            return _organizationSupplierService.RemoveSupplierFromOrganization(organizationUuid, supplierUuid)
                .Match(FromOperationError, Ok);
        }

        private static IEnumerable<ShallowOrganizationResponseDTO> MapToResponse(IEnumerable<OrganizationSupplier> suppliers)
        {
            return suppliers.Select(MapSingleToResponse).ToList();
        }
        private static ShallowOrganizationResponseDTO MapSingleToResponse(OrganizationSupplier supplier)
        {
            return supplier.Supplier.MapShallowOrganizationResponseDTO();
        }
    }
}