using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddSystemCriticalityLastChanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CriticalityInfo_LastChanged",
                table: "ItSystemUsage",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItSystemUsage_IsSociallyCritical",
                table: "ItSystemUsage",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItSystemUsage_isBusinessCritical",
                table: "ItSystemUsage",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CriticalityInfo_LastChanged",
                table: "ItSystemUsage");

            migrationBuilder.DropColumn(
                name: "ItSystemUsage_IsSociallyCritical",
                table: "ItSystemUsage");

            migrationBuilder.DropColumn(
                name: "ItSystemUsage_isBusinessCritical",
                table: "ItSystemUsage");
        }
    }
}
