using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddTechnicalSystemType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var isSqlServer = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";
            var maxTextType = isSqlServer ? "nvarchar(max)" : "text";
            var varchar150Type = isSqlServer ? "nvarchar(150)" : "character varying(150)";
            var datetimeType = isSqlServer ? "datetime2" : "timestamp without time zone";
            var uuidType = isSqlServer ? "uniqueidentifier" : "uuid";
            var boolType = isSqlServer ? "bit" : "boolean";

            migrationBuilder.AddColumn<string>(
                name: "TechnicalSystemTypeName",
                table: "ItSystemUsageOverviewReadModels",
                type: maxTextType,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TechnicalSystemTypeUuid",
                table: "ItSystemUsageOverviewReadModels",
                type: uuidType,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TechnicalSystemTypeId",
                table: "ItSystemUsage",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LocalTechnicalSystemTypes",
                columns: table => new
                {
                    Id = isSqlServer
                        ? table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1")
                        : table.Column<int>(type: "integer", nullable: false)
                            .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ObjectOwnerId = table.Column<int>(type: "int", nullable: true),
                    LastChanged = table.Column<DateTime>(type: datetimeType, nullable: false),
                    LastChangedByUserId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: maxTextType, nullable: true),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: boolType, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalTechnicalSystemTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalTechnicalSystemTypes_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LocalTechnicalSystemTypes_User_LastChangedByUserId",
                        column: x => x.LastChangedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LocalTechnicalSystemTypes_User_ObjectOwnerId",
                        column: x => x.ObjectOwnerId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TechnicalSystemTypes",
                columns: table => new
                {
                    Id = isSqlServer
                        ? table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1")
                        : table.Column<int>(type: "integer", nullable: false)
                            .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ObjectOwnerId = table.Column<int>(type: "int", nullable: false),
                    LastChanged = table.Column<DateTime>(type: datetimeType, nullable: false),
                    LastChangedByUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: varchar150Type, maxLength: 150, nullable: false),
                    IsLocallyAvailable = table.Column<bool>(type: boolType, nullable: false),
                    IsObligatory = table.Column<bool>(type: boolType, nullable: false),
                    Description = table.Column<string>(type: maxTextType, nullable: true),
                    IsEnabled = table.Column<bool>(type: boolType, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Uuid = table.Column<Guid>(type: uuidType, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalSystemTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicalSystemTypes_User_LastChangedByUserId",
                        column: x => x.LastChangedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicalSystemTypes_User_ObjectOwnerId",
                        column: x => x.ObjectOwnerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ItSystemUsageOverviewReadModel_Index_TechnicalSystemTypeUuid",
                table: "ItSystemUsageOverviewReadModels",
                column: "TechnicalSystemTypeUuid");

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemUsage_TechnicalSystemTypeId",
                table: "ItSystemUsage",
                column: "TechnicalSystemTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalTechnicalSystemTypes_LastChangedByUserId",
                table: "LocalTechnicalSystemTypes",
                column: "LastChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalTechnicalSystemTypes_ObjectOwnerId",
                table: "LocalTechnicalSystemTypes",
                column: "ObjectOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalTechnicalSystemTypes_OrganizationId",
                table: "LocalTechnicalSystemTypes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalSystemTypes_LastChangedByUserId",
                table: "TechnicalSystemTypes",
                column: "LastChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalSystemTypes_ObjectOwnerId",
                table: "TechnicalSystemTypes",
                column: "ObjectOwnerId");

            migrationBuilder.CreateIndex(
                name: "UX_Option_Uuid",
                table: "TechnicalSystemTypes",
                column: "Uuid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ItSystemUsage_TechnicalSystemTypes_TechnicalSystemTypeId",
                table: "ItSystemUsage",
                column: "TechnicalSystemTypeId",
                principalTable: "TechnicalSystemTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItSystemUsage_TechnicalSystemTypes_TechnicalSystemTypeId",
                table: "ItSystemUsage");

            migrationBuilder.DropTable(
                name: "LocalTechnicalSystemTypes");

            migrationBuilder.DropTable(
                name: "TechnicalSystemTypes");

            migrationBuilder.DropIndex(
                name: "ItSystemUsageOverviewReadModel_Index_TechnicalSystemTypeUuid",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropIndex(
                name: "IX_ItSystemUsage_TechnicalSystemTypeId",
                table: "ItSystemUsage");

            migrationBuilder.DropColumn(
                name: "TechnicalSystemTypeName",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "TechnicalSystemTypeUuid",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "TechnicalSystemTypeId",
                table: "ItSystemUsage");

        }
    }
}
