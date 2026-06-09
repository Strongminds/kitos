using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class Add_ItSystemUsage_CriticalityFieldsLastChanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var isSqlServer = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";
            var datetimeType = isSqlServer ? "datetime2" : "timestamp without time zone";

            migrationBuilder.AddColumn<DateTime>(
                name: "CriticalityFieldsLastChanged",
                table: "ItSystemUsageOverviewReadModels",
                type: datetimeType,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriticalityFieldsLastChanged",
                table: "ItSystemUsage",
                type: datetimeType,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CriticalityFieldsLastChanged",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "CriticalityFieldsLastChanged",
                table: "ItSystemUsage");
        }
    }
}
