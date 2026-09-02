namespace Presentation.Web.Models.API.V2.Response.Supplier;

public class SupplierAssociatedFieldConfigurationResponseDTO
{
    public required string FieldKey { get; set; }
    public FieldControlStateChoice ControlState { get; set; }
}