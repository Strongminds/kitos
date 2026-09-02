using System.ComponentModel.DataAnnotations;
using Presentation.Web.Models.API.V2.Internal.Response.Organizations.Suppliers;

namespace Presentation.Web.Models.API.V2.Internal.Request.Organizations.Suppliers
{
    public class SupplierAssociatedFieldConfigurationRequestDto
    {
        [Required]
        public required string FieldKey { get; set; }

        [Required]
        public required SupplierAssociatedFieldControlStateOption ControlState { get; set; }
    }
}
