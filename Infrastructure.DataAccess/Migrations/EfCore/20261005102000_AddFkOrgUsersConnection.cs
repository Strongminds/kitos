using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddFkOrgUsersConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FkOrgUsersConnected",
                schema: "dbo",
                table: "Organization",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FkOrgUsersConnectedAt",
                schema: "dbo",
                table: "Organization",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FkOrgUsersConnectedByUserId",
                schema: "dbo",
                table: "Organization",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organization_FkOrgUsersConnectedByUserId",
                schema: "dbo",
                table: "Organization",
                column: "FkOrgUsersConnectedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organization_User_FkOrgUsersConnectedByUserId",
                schema: "dbo",
                table: "Organization",
                column: "FkOrgUsersConnectedByUserId",
                principalSchema: "dbo",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organization_User_FkOrgUsersConnectedByUserId",
                schema: "dbo",
                table: "Organization");

            migrationBuilder.DropIndex(
                name: "IX_Organization_FkOrgUsersConnectedByUserId",
                schema: "dbo",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "FkOrgUsersConnected",
                schema: "dbo",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "FkOrgUsersConnectedAt",
                schema: "dbo",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "FkOrgUsersConnectedByUserId",
                schema: "dbo",
                table: "Organization");
        }
    }
}
