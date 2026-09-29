using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PubSub.Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUserChangeDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserChangeDeliveries",
                columns: table => new
                {
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeadLettered = table.Column<bool>(type: "boolean", nullable: false),
                    LastStatusCode = table.Column<int>(type: "integer", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChangeDeliveries", x => x.Uuid);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserChangeDeliveries_DeadLettered_DeliveredAt_NextAttemptAt",
                table: "UserChangeDeliveries",
                columns: new[] { "DeadLettered", "DeliveredAt", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserChangeDeliveries_ExternalMessageId",
                table: "UserChangeDeliveries",
                column: "ExternalMessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserChangeDeliveries");
        }
    }
}
