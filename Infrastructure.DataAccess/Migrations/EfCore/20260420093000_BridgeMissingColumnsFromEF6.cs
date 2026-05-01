using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class BridgeMissingColumnsFromEF6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // These columns exist in the EF Core model but were never added by EF6 migrations on
            // existing databases. All other structural differences (shadow FK column names such as
            // ItSystemUsage_Id, TaskRef_Id, etc.) use EF6's naming convention, which EF Core inherits
            // as shadow property names — so existing EF6 databases already have the correct column names
            // and no renaming is required here.

            migrationBuilder.AddColumn<int>(
                name: "SensitivePersonalDataTypeId",
                table: "ItSystem",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ItSystem_SensitivePersonalDataTypes_SensitivePersonalDataTypeId",
                table: "ItSystem",
                column: "SensitivePersonalDataTypeId",
                principalTable: "SensitivePersonalDataTypes",
                principalColumn: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ItSystem_SensitivePersonalDataTypeId",
                table: "ItSystem",
                column: "SensitivePersonalDataTypeId");

            migrationBuilder.AddColumn<int>(
                name: "RegisterTypeId",
                table: "ItSystemUsage",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ItSystemUsage_RegisterTypes_RegisterTypeId",
                table: "ItSystemUsage",
                column: "RegisterTypeId",
                principalTable: "RegisterTypes",
                principalColumn: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemUsage_RegisterTypeId",
                table: "ItSystemUsage",
                column: "RegisterTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItSystemUsage_RegisterTypeId",
                table: "ItSystemUsage");

            migrationBuilder.DropForeignKey(
                name: "FK_ItSystemUsage_RegisterTypes_RegisterTypeId",
                table: "ItSystemUsage");

            migrationBuilder.DropColumn(
                name: "RegisterTypeId",
                table: "ItSystemUsage");

            migrationBuilder.DropIndex(
                name: "IX_ItSystem_SensitivePersonalDataTypeId",
                table: "ItSystem");

            migrationBuilder.DropForeignKey(
                name: "FK_ItSystem_SensitivePersonalDataTypes_SensitivePersonalDataTypeId",
                table: "ItSystem");

            migrationBuilder.DropColumn(
                name: "SensitivePersonalDataTypeId",
                table: "ItSystem");
        }
    }
}
