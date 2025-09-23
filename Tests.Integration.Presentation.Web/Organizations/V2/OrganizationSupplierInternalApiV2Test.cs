using System.Threading.Tasks;
using Presentation.Web.Models.API.V2.Types.Organization;
using Tests.Integration.Presentation.Web.Tools.Internal.Organizations;
using Xunit;

namespace Tests.Integration.Presentation.Web.Organizations.V2
{
    public class OrganizationSupplierInternalApiV2Test : OrganizationApiV2TestBase
    {
        [Fact]
        public async Task Can_Perform_Add_Get_Delete_On_Suppliers()
        {
            var organization = await CreateOrganizationAsync();
            var supplier = await CreateOrganizationAsync(type: OrganizationType.Company, isSupplier: true);

            await OrganizationSupplierInternalV2Helper.AddSupplier(organization.Uuid, supplier.Uuid);

            var response = await OrganizationSupplierInternalV2Helper.GetSuppliers(organization.Uuid);

            var responseSupplier = Assert.Single(response);
            Assert.Equal(supplier.Uuid, responseSupplier.Uuid);
            Assert.Equal(supplier.Name, responseSupplier.Name);
            Assert.Equal(supplier.Cvr, responseSupplier.Cvr);

            await OrganizationSupplierInternalV2Helper.DeleteSupplier(organization.Uuid, supplier.Uuid);

            var responseAfterDelete = await OrganizationSupplierInternalV2Helper.GetSuppliers(organization.Uuid);
            Assert.Empty(responseAfterDelete);
        }
    }
}
