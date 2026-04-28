using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class UpdateIsBusinessCriticalPropertyName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItSystemUsage_IsSociallyCritical",
                table: "ItSystemUsage");

            migrationBuilder.DropColumn(
                name: "ItSystemUsage_isBusinessCritical",
                table: "ItSystemUsage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
