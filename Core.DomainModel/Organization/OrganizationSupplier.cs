namespace Core.DomainModel.Organization
{
    public class OrganizationSupplier
    {
        public int OrganizationId { get; set; }
        public Organization Organization { get; set; }
        public int SupplierId { get; set; }
        public Organization Supplier { get; set; }
    }
}
