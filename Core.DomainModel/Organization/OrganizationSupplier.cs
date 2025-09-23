namespace Core.DomainModel.Organization
{
    public class OrganizationSupplier
    {
        public int OrganizationId { get; set; }
        public Organization Organization { get; set; }
        public int SupplierId { get; set; }
        public Organization Supplier { get; set; }

        public static OrganizationSupplier CreateSupplier(int organizationId, int supplierId)
        {
            return new OrganizationSupplier
            {
                OrganizationId = organizationId,
                SupplierId = supplierId
            };
        }
    }
}
