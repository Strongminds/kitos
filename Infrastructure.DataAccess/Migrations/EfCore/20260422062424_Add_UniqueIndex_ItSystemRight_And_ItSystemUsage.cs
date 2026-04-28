using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class Add_UniqueIndex_ItSystemRight_And_ItSystemUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ItSystemUsage_ItSystemId' AND object_id = OBJECT_ID('dbo.ItSystemUsage'))
                    DROP INDEX IX_ItSystemUsage_ItSystemId ON ItSystemUsage;");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ItSystemRights_ObjectId' AND object_id = OBJECT_ID('dbo.ItSystemRights'))
                    DROP INDEX IX_ItSystemRights_ObjectId ON ItSystemRights;");

            migrationBuilder.CreateIndex(
                name: "UX_ItSystemUsage_ItSystemId_OrganizationId",
                table: "ItSystemUsage",
                columns: new[] { "ItSystemId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ItSystemRight_ObjectId_RoleId_UserId",
                table: "ItSystemRights",
                columns: new[] { "ObjectId", "RoleId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ItSystemUsage_ItSystemId_OrganizationId",
                table: "ItSystemUsage");

            migrationBuilder.DropIndex(
                name: "UX_ItSystemRight_ObjectId_RoleId_UserId",
                table: "ItSystemRights");

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemUsage_ItSystemId",
                table: "ItSystemUsage",
                column: "ItSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemRights_ObjectId",
                table: "ItSystemRights",
                column: "ObjectId");
        }
    }
}
