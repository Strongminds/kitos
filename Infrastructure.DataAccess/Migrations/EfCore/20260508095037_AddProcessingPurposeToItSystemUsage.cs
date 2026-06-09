using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddProcessingPurposeToItSystemUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var isSqlServer = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";
            var maxTextType = isSqlServer ? "nvarchar(max)" : "text";
            var varchar200Type = isSqlServer ? "nvarchar(200)" : "character varying(200)";

            migrationBuilder.AddColumn<string>(
                name: "ProcessingPurpose",
                table: "ItSystemUsageOverviewReadModels",
                type: varchar200Type,
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingPurpose",
                table: "ItSystemUsage",
                type: maxTextType,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ItSystemUsageOverviewReadModel_Index_ProcessingPurpose",
                table: "ItSystemUsageOverviewReadModels",
                column: "ProcessingPurpose");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ItSystemUsageOverviewReadModel_Index_ProcessingPurpose",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "ProcessingPurpose",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "ProcessingPurpose",
                table: "ItSystemUsage");
        }
    }
}
