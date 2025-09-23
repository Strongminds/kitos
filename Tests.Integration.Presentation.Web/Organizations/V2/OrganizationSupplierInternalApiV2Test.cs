using System.Threading.Tasks;
using Presentation.Web.Models.API.V2.Types.Organization;
using Tests.Integration.Presentation.Web.Tools.Internal.Organizations;
using Xunit;

namespace Tests.Integration.Presentation.Web.Organizations.V2
{
    public class OrganizationSupplierInternalApiV2Test : OrganizationApiV2TestBase
    {
        [Fact]
        public async Task Can_Add_And_Get_Suppliers()
        {
            //Arrange
            var organization = await CreateOrganizationAsync();
            var supplier = await CreateOrganizationAsync(type: OrganizationType.Company, isSupplier: true);

            //Act
            await OrganizationSupplierInternalV2Helper.AddSupplier(organization.Uuid, supplier.Uuid);

            var response = await OrganizationSupplierInternalV2Helper.GetSuppliers(organization.Uuid);

            //Assert
            var responseSupplier = Assert.Single(response);
            Assert.Equal(supplier.Uuid, responseSupplier.Uuid);
            Assert.Equal(supplier.Name, responseSupplier.Name);
            Assert.Equal(supplier.Cvr, responseSupplier.Cvr);
        }
    }
}
