using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class MoveLicensingAndCodeModelsToSystemUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicensingAndCodeModels",
                table: "ItSystem");

            migrationBuilder.AddColumn<string>(
                name: "LicensingAndCodeModels",
                table: "ItSystemUsage",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicensingAndCodeModels",
                table: "ItSystemUsage");

            migrationBuilder.AddColumn<string>(
                name: "LicensingAndCodeModels",
                table: "ItSystem",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
