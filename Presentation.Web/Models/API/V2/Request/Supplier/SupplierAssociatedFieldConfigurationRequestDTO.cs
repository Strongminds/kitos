using System.Collections.Generic;

namespace Presentation.Web.Models.API.V2.Request.Supplier
{
    public class SupplierAssociatedFieldConfigurationRequestDTO
    {
        public IEnumerable<SupplierAssociatedFieldConfigurationItemDTO> Configurations { get; set; }
    }

    public class SupplierAssociatedFieldConfigurationItemDTO
    {
        public string FieldKey { get; set; }
        public string ControlState { get; set; }
    }
}
