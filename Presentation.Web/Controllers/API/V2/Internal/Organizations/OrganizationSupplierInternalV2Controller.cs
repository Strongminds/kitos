using Core.ApplicationServices.Organizations.Write;
using Core.DomainModel.Organization;
using Core.DomainModel.SupplierAssociatedFields;
using Presentation.Web.Controllers.API.V2.Common.Mapping;
using Presentation.Web.Infrastructure.Attributes;
using Presentation.Web.Models.API.V2.Internal.Request.Organizations.Suppliers;
using Presentation.Web.Models.API.V2.Response.Organization;
using Presentation.Web.Models.API.V2.Response.Supplier;
using Presentation.Web.Models.API.V2.Request.Supplier;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Core.DomainModel.SupplierAssociatedFields;
using Presentation.Web.Models.API.V2.Internal.Response.Organizations.Suppliers;

namespace Presentation.Web.Controllers.API.V2.Internal.Organizations
{
    /// <summary>
    /// Internal API for the organizations in KITOS
    /// </summary>
    [Route("api/v2/internal/organizations")]
    public class OrganizationSupplierInternalV2Controller : InternalApiV2Controller
    {
        private readonly IOrganizationSupplierService _organizationSupplierService;

        public OrganizationSupplierInternalV2Controller(IOrganizationSupplierService organizationSupplierService)
        {
            _organizationSupplierService = organizationSupplierService;
        }

        [HttpGet]
        [Route("{organizationUuid}/suppliers/fields")]
        [ApiResponse(typeof(IEnumerable<SupplierAssociatedFieldConfigurationResponseDto>), HttpStatusCode.OK)]
        [ApiResponse(HttpStatusCode.Unauthorized)]
        public IActionResult GetSupplierFieldConfigurations([NonEmptyGuid] Guid organizationUuid)
        {
            var configurations = _organizationSupplierService.GetSupplierFieldConfigurations(organizationUuid)
                .Select(MapSupplierAssociatedFieldConfiguration).ToList();
            return Ok(configurations);
        }

        [HttpPut]
        [Route("{organizationUuid}/suppliers/fields")]
        [ApiResponse(typeof(IEnumerable<SupplierAssociatedFieldConfigurationResponseDto>), HttpStatusCode.OK)]
        [ApiResponse(HttpStatusCode.NotFound)]
        [ApiResponse(HttpStatusCode.BadRequest)]
        [ApiResponse(HttpStatusCode.Unauthorized)]
        public IActionResult UpsertSupplierFieldConfigurations(
            [NonEmptyGuid] Guid organizationUuid,
            [FromBody] IEnumerable<SupplierAssociatedFieldConfigurationRequestDto> fieldConfigurations)
        {
            if (!ModelState.IsValid) return BadRequest();

            var configurations = fieldConfigurations?.Select(MapSupplierAssociatedFieldConfiguration).ToList()
                ?? new List<SupplierAssociatedFieldConfiguration>();

            return _organizationSupplierService.UpsertSupplierFieldConfigurations(organizationUuid, configurations)
                .Select(value => value.Select(MapSupplierAssociatedFieldConfiguration))
                .Match(Ok, FromOperationError);
        }

        [HttpGet]
        [Route("{organizationUuid}/suppliers")]
        [ApiResponse(typeof(IEnumerable<ShallowOrganizationResponseDTO>), HttpStatusCode.OK)]
        [ApiResponse(HttpStatusCode.NotFound)]
        [ApiResponse(HttpStatusCode.BadRequest)]
        [ApiResponse(HttpStatusCode.Unauthorized)]
        public IActionResult GetSuppliers([NonEmptyGuid] Guid organizationUuid)
        {
            return _organizationSupplierService.GetSuppliersForOrganization(organizationUuid)
                .Select(MapSuppliersToResponse)
                .Match(Ok, FromOperationError);
        }

        [HttpGet]
        [Route("{organizationUuid}/suppliers/available")]
        [ApiResponse(typeof(IEnumerable<ShallowOrganizationResponseDTO>), HttpStatusCode.OK)]
        [ApiResponse(HttpStatusCode.NotFound)]
        [ApiResponse(HttpStatusCode.BadRequest)]
        [ApiResponse(HttpStatusCode.Unauthorized)]
        public IActionResult GetAvailableSuppliers([NonEmptyGuid] Guid organizationUuid)
        {
            return _organizationSupplierService.GetAvailableSuppliers(organizationUuid)
                .Select(MapOrganizations)
                .Match(Ok, FromOperationError);
        }

        [HttpGet]
        [Route("suppliers/{supplierUuid}/using-organizations")]
        [ApiResponse(typeof(IEnumerable<ShallowOrganizationResponseDTO>), HttpStatusCode.OK)]
        [ApiResponse(HttpStatusCode.NotFound)]
        [ApiResponse(HttpStatusCode.BadRequest)]
        [ApiResponse(HttpStatusCode.Unauthorized)]
        public IActionResult GetUsingOrganizations([NonEmptyGuid] Guid supplierUuid)
        {
            return _organizationSupplierService.GetUsingOrganizations(supplierUuid)
                .Select(MapOrganizations)
                .Match(Ok, FromOperationError);
        }

        [HttpPost]
        [Route("{organizationUuid}/suppliers/{supplierUuid}")]
        [ApiResponse(typeof(ShallowOrganizationResponseDTO), HttpStatusCode.OK)]
        [ApiResponse(HttpStatusCode.NotFound)]
        [ApiResponse(HttpStatusCode.BadRequest)]
        [ApiResponse(HttpStatusCode.Unauthorized)]
        public IActionResult AddSupplier([NonEmptyGuid] Guid organizationUuid, [NonEmptyGuid] Guid supplierUuid)
        {
            return _organizationSupplierService.AddSupplierToOrganization(organizationUuid, supplierUuid)
                .Select(MapSingleToResponse)
                .Match(Ok, FromOperationError);
        }

        [HttpDelete]
        [Route("{organizationUuid}/suppliers/{supplierUuid}")]
        [ApiResponse(HttpStatusCode.OK)]
        [ApiResponse(HttpStatusCode.NotFound)]
        [ApiResponse(HttpStatusCode.BadRequest)]
        [ApiResponse(HttpStatusCode.Unauthorized)]
        public IActionResult DeleteSupplier([NonEmptyGuid] Guid organizationUuid, [NonEmptyGuid] Guid supplierUuid)
        {
            return _organizationSupplierService.RemoveSupplierFromOrganization(organizationUuid, supplierUuid)
                .Match(FromOperationError, Ok);
        }

        [HttpGet]
        [Route("{organizationUuid}/suppliers/fields")]
        [ApiResponse(typeof(IEnumerable<SupplierAssociatedFieldConfigurationResponseDTO>), HttpStatusCode.OK)]
        [ApiResponse(HttpStatusCode.NotFound)]
        [ApiResponse(HttpStatusCode.BadRequest)]
        [ApiResponse(HttpStatusCode.Unauthorized)]
        public IActionResult GetSupplierFields([NonEmptyGuid] Guid organizationUuid)
        {
            var configurations = _organizationSupplierService.GetSupplierFieldConfigurations(organizationUuid);
            return Ok(configurations.Select(MapSupplierAssociatedFieldConfiguration).ToList());
        }

        [HttpPut]
        [Route("{organizationUuid}/suppliers/fields")]
        [ApiResponse(typeof(IEnumerable<SupplierAssociatedFieldConfigurationResponseDTO>), HttpStatusCode.OK)]
        [ApiResponse(HttpStatusCode.NotFound)]
        [ApiResponse(HttpStatusCode.BadRequest)]
        [ApiResponse(HttpStatusCode.Unauthorized)]
        public IActionResult PutSupplierFields([NonEmptyGuid] Guid organizationUuid, [FromBody] SupplierAssociatedFieldConfigurationRequestDTO request)
        {
            if (!ModelState.IsValid) return BadRequest();

            var requestConfigurations = request.Configurations
                .Select(c => new SupplierAssociatedFieldConfiguration
                {
                    FieldKey = c.FieldKey,
                    ControlState = Enum.Parse<SupplierAssociatedFieldControlState>(c.ControlState)
                })
                .ToList();

            return _organizationSupplierService.UpsertSupplierFieldConfigurations(organizationUuid, requestConfigurations)
                .Select(configs => configs.Select(MapSupplierAssociatedFieldConfiguration).ToList())
                .Match(Ok, FromOperationError);
        }

        private static IEnumerable<ShallowOrganizationResponseDTO> MapOrganizations(IEnumerable<Organization> organizations)
        {
            return organizations.Select(x => x.MapShallowOrganizationResponseDTO()).ToList();
        }

        private static IEnumerable<ShallowOrganizationResponseDTO> MapSuppliersToResponse(IEnumerable<OrganizationSupplier> suppliers)
        {
            return suppliers.Select(MapSingleToResponse).ToList();
        }

        private static ShallowOrganizationResponseDTO MapSingleToResponse(OrganizationSupplier supplier)
        {
            return supplier.Supplier.MapShallowOrganizationResponseDTO();
        }

        private static SupplierAssociatedFieldConfigurationResponseDto MapSupplierAssociatedFieldConfiguration(SupplierAssociatedFieldConfiguration domainModel)
        {
            return new SupplierAssociatedFieldConfigurationResponseDto
            {
                FieldKey = domainModel.FieldKey,
                ControlState = ToOption(domainModel.ControlState)
            };
        }

        private static SupplierAssociatedFieldConfiguration MapSupplierAssociatedFieldConfiguration(
            SupplierAssociatedFieldConfigurationRequestDto request)
        {
            return new SupplierAssociatedFieldConfiguration
            {
                FieldKey = request.FieldKey,
                ControlState = ToDomain(request.ControlState)
            };
        }

        private static SupplierAssociatedFieldControlStateOption ToOption(SupplierAssociatedFieldControlState controlState) {
            return controlState switch
            {
                SupplierAssociatedFieldControlState.ORGANIZATION => SupplierAssociatedFieldControlStateOption.ORGANIZATION,
                SupplierAssociatedFieldControlState.SUPPLIER => SupplierAssociatedFieldControlStateOption.SUPPLIER,
                SupplierAssociatedFieldControlState.SHARED => SupplierAssociatedFieldControlStateOption.SHARED,
                _ => throw new ArgumentOutOfRangeException(nameof(controlState), controlState, "Invalid value passed as controlState for mapping.")
            };
        }

        private static SupplierAssociatedFieldControlState ToDomain(SupplierAssociatedFieldControlStateOption controlState)
        {
            return controlState switch
            {
                SupplierAssociatedFieldControlStateOption.ORGANIZATION => SupplierAssociatedFieldControlState.ORGANIZATION,
                SupplierAssociatedFieldControlStateOption.SUPPLIER => SupplierAssociatedFieldControlState.SUPPLIER,
                SupplierAssociatedFieldControlStateOption.SHARED => SupplierAssociatedFieldControlState.SHARED,
                _ => throw new ArgumentOutOfRangeException(nameof(controlState), controlState, "Invalid value passed as controlState for mapping.")
            };
        }
    }
}
