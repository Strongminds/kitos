using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PubSub.Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUserChangeReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RabbitMqMigratedAt",
                table: "UserChangeDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserChangeReceipts",
                columns: table => new
                {
                    ExternalMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventHash = table.Column<string>(type: "character varying(67)", maxLength: 67, nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChangeReceipts", x => x.ExternalMessageId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserChangeReceipts_AcceptedAt_ReservedAt",
                table: "UserChangeReceipts",
                columns: new[] { "AcceptedAt", "ReservedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserChangeReceipts");

            migrationBuilder.DropColumn(
                name: "RabbitMqMigratedAt",
                table: "UserChangeDeliveries");
        }
    }
}
