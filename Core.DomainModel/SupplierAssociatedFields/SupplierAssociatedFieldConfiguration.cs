namespace Core.DomainModel.SupplierAssociatedFields
{
    public class SupplierAssociatedFieldConfiguration: Entity, IOwnedByOrganization
    {
        public string FieldKey { get; set; }
        public SupplierAssociatedFieldControlState ControlState { get; set; }
        public int OrganizationId { get; set; }
        public virtual Organization.Organization Organization { get; set; }

        public bool HasSupplierControlState => ControlState == SupplierAssociatedFieldControlState.Supplier;
        public bool HasSharedControlState => ControlState == SupplierAssociatedFieldControlState.Shared;
    }
}
