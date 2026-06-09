using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddIsDataProcessingAgreementRequired_ToItSystemUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IsDataProcessingAgreementRequired",
                table: "ItSystemUsageOverviewReadModels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsDataProcessingAgreementRequired",
                table: "ItSystemUsage",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDataProcessingAgreementRequired",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "IsDataProcessingAgreementRequired",
                table: "ItSystemUsage");
        }
    }
}
