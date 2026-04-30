using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddSupplierDisabledToReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var columnType = ActiveProvider.Contains("Npgsql", System.StringComparison.OrdinalIgnoreCase)
                ? "boolean"
                : "bit";

            migrationBuilder.AddColumn<bool>(
                name: "IsSupplierDisabled",
                table: "ItContractOverviewReadModels",
                type: columnType,
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSupplierDisabled",
                table: "ItContractOverviewReadModels");
        }
    }
}
