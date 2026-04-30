using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class MakeSystemUsageCriticalityChoiceType_InsteadOfGdprCriticalityEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ItSystemUsageOverviewReadModel_Index_GdprCriticality",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "GdprCriticality",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.RenameColumn(
                name: "GdprCriticality",
                table: "ItSystemUsage",
                newName: "SystemUsageCriticalityLevelId");

            migrationBuilder.RenameIndex(
                name: "ItSystemUsage_Index_GdprCriticality",
                table: "ItSystemUsage",
                newName: "IX_ItSystemUsage_SystemUsageCriticalityLevelId");

            migrationBuilder.AddColumn<string>(
                name: "CriticalityLevelDocumentationUrl",
                table: "ItSystemUsageOverviewReadModels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CriticalityLevelDocumentationUrlName",
                table: "ItSystemUsageOverviewReadModels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SystemUsageCriticalityLevelUuid",
                table: "ItSystemUsageOverviewReadModels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CriticalityLevelDocumentationName",
                table: "ItSystemUsage",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CriticalityLevelDocumentationUrl",
                table: "ItSystemUsage",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LocalSystemUsageCriticalityLevelTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectOwnerId = table.Column<int>(type: "int", nullable: true),
                    LastChanged = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastChangedByUserId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalSystemUsageCriticalityLevelTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalSystemUsageCriticalityLevelTypes_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LocalSystemUsageCriticalityLevelTypes_User_LastChangedByUserId",
                        column: x => x.LastChangedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LocalSystemUsageCriticalityLevelTypes_User_ObjectOwnerId",
                        column: x => x.ObjectOwnerId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SystemUsageCriticalityLevelTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectOwnerId = table.Column<int>(type: "int", nullable: false),
                    LastChanged = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastChangedByUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsLocallyAvailable = table.Column<bool>(type: "bit", nullable: false),
                    IsObligatory = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Uuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemUsageCriticalityLevelTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemUsageCriticalityLevelTypes_User_LastChangedByUserId",
                        column: x => x.LastChangedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemUsageCriticalityLevelTypes_User_ObjectOwnerId",
                        column: x => x.ObjectOwnerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ItSystemUsageOverviewReadModel_Index_SystemUsageCriticalityLevelUuid",
                table: "ItSystemUsageOverviewReadModels",
                column: "SystemUsageCriticalityLevelUuid");

            migrationBuilder.CreateIndex(
                name: "IX_LocalSystemUsageCriticalityLevelTypes_LastChangedByUserId",
                table: "LocalSystemUsageCriticalityLevelTypes",
                column: "LastChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalSystemUsageCriticalityLevelTypes_ObjectOwnerId",
                table: "LocalSystemUsageCriticalityLevelTypes",
                column: "ObjectOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalSystemUsageCriticalityLevelTypes_OrganizationId",
                table: "LocalSystemUsageCriticalityLevelTypes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUsageCriticalityLevelTypes_LastChangedByUserId",
                table: "SystemUsageCriticalityLevelTypes",
                column: "LastChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUsageCriticalityLevelTypes_ObjectOwnerId",
                table: "SystemUsageCriticalityLevelTypes",
                column: "ObjectOwnerId");

            migrationBuilder.CreateIndex(
                name: "UX_Option_Uuid",
                table: "SystemUsageCriticalityLevelTypes",
                column: "Uuid",
                unique: true);

            migrationBuilder.Sql(@"
                -- If a global admin user exists, seed the five criticality level option types, mirroring the old GdprCriticality enum values.
                IF EXISTS (SELECT 1 FROM dbo.[User] WHERE IsGlobalAdmin = 1)
                BEGIN
                    INSERT INTO dbo.SystemUsageCriticalityLevelTypes
                        (ObjectOwnerId, LastChanged, LastChangedByUserId, Name, IsLocallyAvailable, IsObligatory, [Description], IsEnabled, Priority, Uuid)
                    SELECT
                        (SELECT TOP 1 Id FROM dbo.[User] WHERE IsGlobalAdmin = 1 ORDER BY Id),
                        GETUTCDATE(),
                        (SELECT TOP 1 Id FROM dbo.[User] WHERE IsGlobalAdmin = 1 ORDER BY Id),
                        v.Name, v.IsLocallyAvailable, v.IsObligatory, NULL, v.IsEnabled, v.Priority, NEWID()
                    FROM (VALUES
                        (N'Ikke kritisk', 1, 0, 1, 0),
                        (N'Lav',         1, 0, 1, 1),
                        (N'Mellem',       1, 0, 1, 2),
                        (N'Høj',         1, 0, 1, 3),
                        (N'Meget høj',   1, 0, 1, 4)
                    ) AS v(Name, IsLocallyAvailable, IsObligatory, IsEnabled, Priority);
                END

                -- Remap old enum integers to new option type FK IDs
                UPDATE isu
                SET isu.SystemUsageCriticalityLevelId = opt.Id
                FROM dbo.ItSystemUsage isu
                INNER JOIN dbo.SystemUsageCriticalityLevelTypes opt
                    ON opt.Name = CASE isu.SystemUsageCriticalityLevelId
                        WHEN 0 THEN N'Ikke kritisk'
                        WHEN 1 THEN N'Lavt'
                        WHEN 2 THEN N'Mellem'
                        WHEN 3 THEN N'Højt'
                        WHEN 4 THEN N'Meget højt'
                    END
                WHERE isu.SystemUsageCriticalityLevelId IS NOT NULL;

                -- Null out any rows whose value was not a recognised enum integer (defensive cleanup)
                UPDATE dbo.ItSystemUsage
                SET SystemUsageCriticalityLevelId = NULL
                WHERE SystemUsageCriticalityLevelId IS NOT NULL
                  AND SystemUsageCriticalityLevelId NOT IN (SELECT Id FROM dbo.SystemUsageCriticalityLevelTypes);
                ");

            migrationBuilder.AddForeignKey(
                name: "FK_ItSystemUsage_SystemUsageCriticalityLevelTypes_SystemUsageCriticalityLevelId",
                table: "ItSystemUsage",
                column: "SystemUsageCriticalityLevelId",
                principalTable: "SystemUsageCriticalityLevelTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItSystemUsage_SystemUsageCriticalityLevelTypes_SystemUsageCriticalityLevelId",
                table: "ItSystemUsage");

            migrationBuilder.DropTable(
                name: "LocalSystemUsageCriticalityLevelTypes");

            migrationBuilder.DropTable(
                name: "SystemUsageCriticalityLevelTypes");

            migrationBuilder.DropIndex(
                name: "ItSystemUsageOverviewReadModel_Index_SystemUsageCriticalityLevelUuid",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "CriticalityLevelDocumentationUrl",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "CriticalityLevelDocumentationUrlName",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "SystemUsageCriticalityLevelUuid",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "CriticalityLevelDocumentationName",
                table: "ItSystemUsage");

            migrationBuilder.DropColumn(
                name: "CriticalityLevelDocumentationUrl",
                table: "ItSystemUsage");

            migrationBuilder.RenameColumn(
                name: "SystemUsageCriticalityLevelId",
                table: "ItSystemUsage",
                newName: "GdprCriticality");

            migrationBuilder.RenameIndex(
                name: "IX_ItSystemUsage_SystemUsageCriticalityLevelId",
                table: "ItSystemUsage",
                newName: "ItSystemUsage_Index_GdprCriticality");

            migrationBuilder.AddColumn<int>(
                name: "GdprCriticality",
                table: "ItSystemUsageOverviewReadModels",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ItSystemUsageOverviewReadModel_Index_GdprCriticality",
                table: "ItSystemUsageOverviewReadModels",
                column: "GdprCriticality");
        }
    }
}
