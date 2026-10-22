using System.Linq;
using Core.Abstractions.Types;
using Core.ApplicationServices.Contract.ReadModels;
using Core.DomainServices.Generic;
using Infrastructure.DataAccess;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Moq;
using Presentation.Web.Controllers.API.V1.OData;
using Core.DomainModel.ItContract.Read;
using Microsoft.OData.Edm;
using Presentation.Web.Infrastructure.OData;
using Xunit;

namespace Tests.Unit.Presentation.Web.Infrastructure
{
    public class ContractOverviewInvoiceDateODataTest
    {
        [Fact]
        public void InvoiceDates_Is_A_Collection_Of_Complex_Month_And_Day_Objects()
        {
            var model = ODataModelConfig.GetEdmModel();
            var overview = (IEdmEntityType)model.FindDeclaredType(typeof(ItContractOverviewReadModel).FullName);
            var property = overview.FindProperty(nameof(ItContractOverviewReadModel.InvoiceDates));
            Assert.Equal(EdmPropertyKind.Structural, property.PropertyKind);
            var date = Assert.IsAssignableFrom<IEdmComplexType>(property.Type.AsCollection().ElementType().Definition);
            Assert.Equal(new[] { "Day", "Month" }, date.Properties().Select(x => x.Name).OrderBy(x => x));
            Assert.All(date.Properties(), x => Assert.Equal(EdmPrimitiveTypeKind.Int32, x.Type.PrimitiveKind()));
        }
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void Period_Fields_Are_Nullable_Int32_And_OData_Queries_Execute_In_Database_Before_Paging(int period)
        {
            var model = ODataModelConfig.GetEdmModel();
            var overview = (IEdmEntityType)model.FindDeclaredType(typeof(ItContractOverviewReadModel).FullName);
            foreach (var part in new[] { "Month", "Day" })
            {
                var property = overview.FindProperty($"InvoiceDate{period}{part}");
                Assert.Equal(EdmPropertyKind.Structural, property.PropertyKind);
                Assert.True(property.Type.IsNullable);
                Assert.Equal(EdmPrimitiveTypeKind.Int32, property.Type.PrimitiveKind());
            }

            var request = new DefaultHttpContext().Request;
            request.QueryString = new QueryString($"?$filter=InvoiceDate{period}Month eq 3 and InvoiceDate{period}Day eq 31&$orderby=InvoiceDate{period}Month asc,InvoiceDate{period}Day asc&$skip=10&$top=5");
            var options = new ODataQueryOptions<ItContractOverviewReadModel>(
                new ODataQueryContext(model, typeof(ItContractOverviewReadModel), null), request);
            using var context = new KitosContext(new DbContextOptionsBuilder<KitosContext>()
                .UseNpgsql("Host=localhost;Database=kitos;Username=postgres").Options);
            var query = (IQueryable<ItContractOverviewReadModel>)options.ApplyTo(context.ItContractOverviewReadModels,
                new ODataQuerySettings { HandleNullPropagation = HandleNullPropagationOption.False, EnableConstantParameterization = false });
            var sql = query.ToQueryString();

            Assert.Contains($"\"InvoiceDate{period}Month\" = 3", sql);
            Assert.Contains($"\"InvoiceDate{period}Day\" = 31", sql);
            Assert.Matches($"ORDER BY [^\\r\\n]*\\\"InvoiceDate{period}Month\\\", [^\\r\\n]*\\\"InvoiceDate{period}Day\\\"", sql);
            Assert.True(sql.IndexOf("WHERE", System.StringComparison.Ordinal) < sql.IndexOf("ORDER BY", System.StringComparison.Ordinal));
            Assert.True(sql.IndexOf("ORDER BY", System.StringComparison.Ordinal) < sql.IndexOf("LIMIT", System.StringComparison.Ordinal));
            Assert.Contains("OFFSET", sql);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Endpoint_Preserves_Queryable_For_OData(bool filterByUnit)
        {
            var query = new[] { new ItContractOverviewReadModel() }.AsQueryable();
            var service = new Mock<IItContractOverviewReadModelsService>();
            var result = Result<IQueryable<ItContractOverviewReadModel>, OperationError>.Success(query);
            service.Setup(x => x.GetByOrganizationId(1)).Returns(result);
            service.Setup(x => x.GetByOrganizationIdOrUnitIdInSubTree(1, 2)).Returns(result);
            var controller = new ItContractOverviewReadModelsController(service.Object, Mock.Of<IEntityIdentityResolver>());

            var response = Assert.IsType<OkObjectResult>(controller.GetByOrganizationId(1, filterByUnit ? 2 : null));

            Assert.Same(query, response.Value);
        }
    }
}
