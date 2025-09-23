using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Response.Organizations;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using Core.ApplicationServices.Organizations.Write;
using Core.DomainModel.Organization;

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

        [HttpGet]
        [Route("{organizationUuid}/suppliers")]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IEnumerable<OrganizationSupplierResponseDTO>))]
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
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(OrganizationSupplierResponseDTO))]
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
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(OrganizationSupplierResponseDTO))]
        [SwaggerResponse(HttpStatusCode.NotFound)]
        [SwaggerResponse(HttpStatusCode.BadRequest)]
        [SwaggerResponse(HttpStatusCode.Unauthorized)]
        public IHttpActionResult DeleteSupplier([NonEmptyGuid] Guid organizationUuid, [NonEmptyGuid] Guid supplierUuid)
        {
            return _organizationSupplierService.RemoveSupplierFromOrganization(organizationUuid, supplierUuid)
                .Match(FromOperationError, Ok);
        }

        private static IEnumerable<OrganizationSupplierResponseDTO> MapToResponse(IEnumerable<OrganizationSupplier> suppliers)
        {
            return suppliers.Select(MapSingleToResponse).ToList();
        }
        private static OrganizationSupplierResponseDTO MapSingleToResponse(OrganizationSupplier supplier)
        {
            return new OrganizationSupplierResponseDTO
            {
                Name = supplier.Supplier.Name,
                Cvr = supplier.Supplier.Cvr
            };
        }
    }
}