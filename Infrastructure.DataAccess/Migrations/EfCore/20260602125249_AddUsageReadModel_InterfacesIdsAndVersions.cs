using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddUsageReadModel_InterfacesIdsAndVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var isSqlServer = ActiveProvider.Contains("SqlServer", System.StringComparison.OrdinalIgnoreCase);
            var maxTextType = isSqlServer ? "nvarchar(max)" : "text";

            migrationBuilder.AddColumn<string>(
                name: "ItInterfaceIdsAsCsv",
                table: "ItSystemUsageOverviewReadModels",
                type: maxTextType,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItInterfaceVersionsAsCsv",
                table: "ItSystemUsageOverviewReadModels",
                type: maxTextType,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItInterfaceIdsAsCsv",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "ItInterfaceVersionsAsCsv",
                table: "ItSystemUsageOverviewReadModels");
        }
    }
}
