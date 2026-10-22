using System;
using System.Linq;
using Core.DomainModel.ItContract;
using Core.DomainModel.ItContract.Read;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.Migrations.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.DataAccess
{
    public class ItContractInvoiceDateMappingTest : WithAutoFixture
    {
        [Fact]
        public void InvoiceDates_PostgreSql_Model_Uses_Day_Month_And_Owned_Unique_Periods()
        {
            using var context = CreateContext();
            var entity = context.Model.FindEntityType(typeof(ItContractInvoiceDate));
            Assert.NotNull(entity);
            Assert.Equal(new[] { nameof(ItContractInvoiceDate.ItContractId), nameof(ItContractInvoiceDate.Period) },
                entity.FindPrimaryKey()!.Properties.Select(x => x.Name));
            Assert.Equal("integer", entity.FindProperty(nameof(ItContractInvoiceDate.Month))!.GetColumnType());
            Assert.Equal("integer", entity.FindProperty(nameof(ItContractInvoiceDate.Day))!.GetColumnType());
            var foreignKey = Assert.Single(entity.GetForeignKeys());
            Assert.Equal(typeof(ItContract), foreignKey.PrincipalEntityType.ClrType);
            Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
            Assert.True(foreignKey.IsRequired);
            var overview = context.Model.FindEntityType(typeof(ItContractOverviewReadModel));
            Assert.NotNull(overview);
            Assert.Equal("InvoiceDates", overview.FindNavigation(nameof(ItContractOverviewReadModel.InvoiceDates))!.TargetEntityType.GetContainerColumnName());
        }

        [Fact]
        public void InvoiceSchedule_Migration_Only_Adds_Compatible_Columns_And_Child_Table()
        {
            var migration = new AddContractInvoiceSchedule { ActiveProvider = "Npgsql.EntityFrameworkCore.PostgreSQL" };
            var columns = migration.UpOperations.OfType<AddColumnOperation>().ToList();
            Assert.Equal(3, columns.Count);
            Assert.All(columns.Where(x => x.Name == "InvoiceFrequency"), x => Assert.True(x.IsNullable));
            var overviewDates = Assert.Single(columns, x => x.Name == "InvoiceDates");
            Assert.False(overviewDates.IsNullable);
            Assert.Equal("", overviewDates.DefaultValue);
            var table = Assert.Single(migration.UpOperations.OfType<CreateTableOperation>());
            Assert.Equal("ItContractInvoiceDates", table.Name);
            Assert.Equal(new[] { "ItContractId", "Period" }, table.PrimaryKey!.Columns);
            Assert.Equal("integer", Assert.Single(table.Columns, x => x.Name == "Month").ColumnType);
            Assert.Equal("integer", Assert.Single(table.Columns, x => x.Name == "Day").ColumnType);
            Assert.Equal(ReferentialAction.Cascade, Assert.Single(table.ForeignKeys).OnDelete);
            Assert.Equal(4, migration.UpOperations.Count);
            using var context = CreateContext();
            var commands = context.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations);
            var sql = string.Join(Environment.NewLine, commands.Select(x => x.CommandText));
            Assert.Contains("CREATE TABLE dbo.\"ItContractInvoiceDates\"", sql);
            Assert.Contains("\"Month\" integer NOT NULL", sql);
            Assert.Contains("\"Day\" integer NOT NULL", sql);
            Assert.Contains("ON DELETE CASCADE", sql);
            Assert.DoesNotContain("ALTER COLUMN", sql);
        }

        [Fact]
        public void Overview_InvoiceDates_Can_Filter_And_Order_By_Numeric_Date_Parts()
        {
            using var context = CreateContext();
            var sql = context.ItContractOverviewReadModels
                .Where(x => x.InvoiceDates.Any(date => date.Month == 2 && date.Day == 29))
                .OrderBy(x => x.InvoiceDates.Min(date => date.Month))
                .ThenBy(x => x.InvoiceDates.Min(date => date.Day))
                .ToQueryString();

            Assert.Contains("InvoiceDates", sql);
            Assert.Contains("Month", sql);
            Assert.Contains("Day", sql);
            Assert.Contains("ORDER BY", sql);
        }

        [Fact]
        public void Overview_Invoice_Periods_Are_Nullable_Integer_Columns_And_Migration_Backfills_All_Periods()
        {
            using var context = CreateContext();
            var overview = context.Model.FindEntityType(typeof(ItContractOverviewReadModel))!;
            var migration = new StructureContractOverviewInvoiceDates();
            var columns = migration.UpOperations.OfType<AddColumnOperation>().ToList();
            Assert.Equal(8, columns.Count);
            foreach (var period in Enumerable.Range(1, 4))
            {
                foreach (var part in new[] { "Month", "Day" })
                {
                    var name = $"InvoiceDate{period}{part}";
                    var property = overview.FindProperty(name)!;
                    Assert.True(property.IsNullable);
                    Assert.Equal("integer", property.GetColumnType());
                    var column = Assert.Single(columns, x => x.Name == name);
                    Assert.True(column.IsNullable);
                    Assert.Equal("integer", column.ColumnType);
                }
                Assert.Contains(migration.UpOperations.OfType<SqlOperation>(), operation =>
                    operation.Sql.Contains($"date.\"Period\" = {period}") &&
                    operation.Sql.Contains($"\"InvoiceDate{period}Month\"") &&
                    operation.Sql.Contains($"\"InvoiceDate{period}Day\""));
            }
            Assert.Equal(8, migration.DownOperations.OfType<DropColumnOperation>().Count());
            Assert.DoesNotContain(migration.UpOperations, x => x is AlterColumnOperation || x is DropColumnOperation);
            var conversion = Assert.IsType<SqlOperation>(migration.UpOperations.First());
            Assert.Contains("jsonb_agg", conversion.Sql);
            Assert.Contains("TYPE jsonb", conversion.Sql);
            var rollback = Assert.IsType<SqlOperation>(migration.DownOperations.Last());
            Assert.Contains("TYPE text", rollback.Sql);
            Assert.Contains("string_agg", rollback.Sql);

            var generator = context.GetService<IMigrationsSqlGenerator>();
            Assert.NotEmpty(generator.Generate(migration.UpOperations));
            Assert.NotEmpty(generator.Generate(migration.DownOperations));
        }

        private static KitosContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<KitosContext>()
                .UseNpgsql("Host=localhost;Database=kitos;Username=postgres");
            return new KitosContext(options.Options);
        }
    }
}
