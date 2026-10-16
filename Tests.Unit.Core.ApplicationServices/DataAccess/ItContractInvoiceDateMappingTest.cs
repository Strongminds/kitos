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
        public void InvoiceDates_PostgreSql_Model_Uses_Date_And_Owned_Unique_Periods()
        {
            using var context = CreateContext();
            var entity = context.Model.FindEntityType(typeof(ItContractInvoiceDate));
            Assert.NotNull(entity);
            Assert.Equal(new[] { nameof(ItContractInvoiceDate.ItContractId), nameof(ItContractInvoiceDate.Period) },
                entity.FindPrimaryKey()!.Properties.Select(x => x.Name));
            Assert.Equal("date", entity.FindProperty(nameof(ItContractInvoiceDate.Date))!.GetColumnType());
            var foreignKey = Assert.Single(entity.GetForeignKeys());
            Assert.Equal(typeof(ItContract), foreignKey.PrincipalEntityType.ClrType);
            Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
            Assert.True(foreignKey.IsRequired);
            var overview = context.Model.FindEntityType(typeof(ItContractOverviewReadModel));
            Assert.NotNull(overview);
            Assert.Equal("", overview.FindProperty(nameof(ItContractOverviewReadModel.InvoiceDates))!.GetDefaultValue());
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
            Assert.Equal("date", Assert.Single(table.Columns, x => x.Name == "Date").ColumnType);
            Assert.Equal(ReferentialAction.Cascade, Assert.Single(table.ForeignKeys).OnDelete);
            Assert.Equal(4, migration.UpOperations.Count);
            using var context = CreateContext();
            var commands = context.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations);
            var sql = string.Join(Environment.NewLine, commands.Select(x => x.CommandText));
            Assert.Contains("CREATE TABLE dbo.\"ItContractInvoiceDates\"", sql);
            Assert.Contains("\"Date\" date NOT NULL", sql);
            Assert.Contains("ON DELETE CASCADE", sql);
            Assert.DoesNotContain("ALTER COLUMN", sql);
        }

        private static KitosContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<KitosContext>()
                .UseNpgsql("Host=localhost;Database=kitos;Username=postgres");
            return new KitosContext(options.Options);
        }
    }
}
