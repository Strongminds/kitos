namespace Presentation.Web.Models.API.V2.Internal.Response.Organizations.Suppliers
{
    public class SupplierAssociatedFieldConfigurationResponseDto
    {
        public required string FieldKey { get; set; }
        public required SupplierAssociatedFieldControlStateOption ControlState { get; set; }
    }
}
