using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class IntroduceArchivingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItSystemUsage_TechnicalSystemTypes_TechnicalSystemTypeId",
                table: "ItSystemUsage");

            migrationBuilder.DropIndex(
                name: "ItSystemUsageOverviewReadModel_Index_TechnicalSystemTypeUuid",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropIndex(
                name: "IX_ItSystemUsage_TechnicalSystemTypeId",
                table: "ItSystemUsage");

            migrationBuilder.DropColumn(
                name: "TechnicalSystemTypeUuid",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "TechnicalSystemTypeId",
                table: "ItSystemUsage");

            migrationBuilder.RenameColumn(
                name: "TechnicalSystemTypeName",
                table: "ItSystemUsageOverviewReadModels",
                newName: "TechnicalSystemTypeNamesAsCsv");

            migrationBuilder.AddColumn<string>(
                name: "ItInterfaceIdsAsCsv",
                table: "ItSystemUsageOverviewReadModels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItInterfaceVersionsAsCsv",
                table: "ItSystemUsageOverviewReadModels",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Organization_Uuid",
                table: "Organization",
                column: "Uuid");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ItSystem_Uuid",
                table: "ItSystem",
                column: "Uuid");

            migrationBuilder.CreateTable(
                name: "ItSystemArchive",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotUuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationUuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArchivingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObjectOwnerId = table.Column<int>(type: "int", nullable: false),
                    LastChanged = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastChangedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItSystemArchive", x => x.Id);
                    table.UniqueConstraint("AK_ItSystemArchive_Uuid", x => x.Uuid);
                    table.ForeignKey(
                        name: "FK_ItSystemArchive_Organization_OrganizationUuid",
                        column: x => x.OrganizationUuid,
                        principalTable: "Organization",
                        principalColumn: "Uuid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItSystemArchive_User_LastChangedByUserId",
                        column: x => x.LastChangedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItSystemArchive_User_ObjectOwnerId",
                        column: x => x.ObjectOwnerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItSystemUsageOverviewTechnicalSystemTypeReadModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnicalSystemTypeUuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnicalSystemTypeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ParentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItSystemUsageOverviewTechnicalSystemTypeReadModel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItSystemUsageOverviewTechnicalSystemTypeReadModel_ItSystemUsageOverviewReadModels_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ItSystemUsageOverviewReadModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItSystemUsageTechnicalSystemTypes",
                columns: table => new
                {
                    ReferencesId = table.Column<int>(type: "int", nullable: false),
                    TechnicalSystemTypesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItSystemUsageTechnicalSystemTypes", x => new { x.ReferencesId, x.TechnicalSystemTypesId });
                    table.ForeignKey(
                        name: "FK_ItSystemUsageTechnicalSystemTypes_ItSystemUsage_ReferencesId",
                        column: x => x.ReferencesId,
                        principalTable: "ItSystemUsage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItSystemUsageTechnicalSystemTypes_TechnicalSystemTypes_TechnicalSystemTypesId",
                        column: x => x.TechnicalSystemTypesId,
                        principalTable: "TechnicalSystemTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArchiveReference",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ItSystemArchiveUuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectOwnerId = table.Column<int>(type: "int", nullable: false),
                    LastChanged = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastChangedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveReference", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchiveReference_ItSystemArchive_ItSystemArchiveUuid",
                        column: x => x.ItSystemArchiveUuid,
                        principalTable: "ItSystemArchive",
                        principalColumn: "Uuid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArchiveReference_User_LastChangedByUserId",
                        column: x => x.LastChangedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArchiveReference_User_ObjectOwnerId",
                        column: x => x.ObjectOwnerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Snapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegacyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItSystemUuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItSystemArchiveUuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectOwnerId = table.Column<int>(type: "int", nullable: false),
                    LastChanged = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastChangedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Snapshot_ItSystemArchive_ItSystemArchiveUuid",
                        column: x => x.ItSystemArchiveUuid,
                        principalTable: "ItSystemArchive",
                        principalColumn: "Uuid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Snapshot_ItSystem_ItSystemUuid",
                        column: x => x.ItSystemUuid,
                        principalTable: "ItSystem",
                        principalColumn: "Uuid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Snapshot_User_LastChangedByUserId",
                        column: x => x.LastChangedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Snapshot_User_ObjectOwnerId",
                        column: x => x.ObjectOwnerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Category_Id",
                table: "PendingReadModelUpdates",
                columns: new[] { "Category", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveReference_ItSystemArchiveUuid",
                table: "ArchiveReference",
                column: "ItSystemArchiveUuid");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveReference_LastChangedByUserId",
                table: "ArchiveReference",
                column: "LastChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveReference_ObjectOwnerId",
                table: "ArchiveReference",
                column: "ObjectOwnerId");

            migrationBuilder.CreateIndex(
                name: "UX_ArchiveReference_Uuid",
                table: "ArchiveReference",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemArchive_LastChangedByUserId",
                table: "ItSystemArchive",
                column: "LastChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemArchive_ObjectOwnerId",
                table: "ItSystemArchive",
                column: "ObjectOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemArchive_OrganizationUuid",
                table: "ItSystemArchive",
                column: "OrganizationUuid");

            migrationBuilder.CreateIndex(
                name: "UX_ItSystemArchive_ItSystemUsageSnapshotUuid",
                table: "ItSystemArchive",
                column: "SnapshotUuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ItSystemArchive_Uuid",
                table: "ItSystemArchive",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ItSystemUsageOverviewTechnicalSystemTypeReadModel_Index_Uuid",
                table: "ItSystemUsageOverviewTechnicalSystemTypeReadModel",
                column: "TechnicalSystemTypeUuid");

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemUsageOverviewTechnicalSystemTypeReadModel_ParentId",
                table: "ItSystemUsageOverviewTechnicalSystemTypeReadModel",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemUsageTechnicalSystemTypes_TechnicalSystemTypesId",
                table: "ItSystemUsageTechnicalSystemTypes",
                column: "TechnicalSystemTypesId");

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemUsageArchiveSnapshot_ItSystemUuid",
                table: "Snapshot",
                column: "ItSystemUuid");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_LastChangedByUserId",
                table: "Snapshot",
                column: "LastChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_ObjectOwnerId",
                table: "Snapshot",
                column: "ObjectOwnerId");

            migrationBuilder.CreateIndex(
                name: "UX_ItSystemUsageArchiveSnapshot_ItSystemArchiveUuid",
                table: "Snapshot",
                column: "ItSystemArchiveUuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ItSystemUsageArchiveSnapshot_Uuid",
                table: "Snapshot",
                column: "Uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchiveReference");

            migrationBuilder.DropTable(
                name: "ItSystemUsageOverviewTechnicalSystemTypeReadModel");

            migrationBuilder.DropTable(
                name: "ItSystemUsageTechnicalSystemTypes");

            migrationBuilder.DropTable(
                name: "Snapshot");

            migrationBuilder.DropTable(
                name: "ItSystemArchive");

            migrationBuilder.DropIndex(
                name: "IX_Category_Id",
                table: "PendingReadModelUpdates");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Organization_Uuid",
                table: "Organization");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ItSystem_Uuid",
                table: "ItSystem");

            migrationBuilder.DropColumn(
                name: "ItInterfaceIdsAsCsv",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "ItInterfaceVersionsAsCsv",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.RenameColumn(
                name: "TechnicalSystemTypeNamesAsCsv",
                table: "ItSystemUsageOverviewReadModels",
                newName: "TechnicalSystemTypeName");

            migrationBuilder.AddColumn<Guid>(
                name: "TechnicalSystemTypeUuid",
                table: "ItSystemUsageOverviewReadModels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TechnicalSystemTypeId",
                table: "ItSystemUsage",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ItSystemUsageOverviewReadModel_Index_TechnicalSystemTypeUuid",
                table: "ItSystemUsageOverviewReadModels",
                column: "TechnicalSystemTypeUuid");

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemUsage_TechnicalSystemTypeId",
                table: "ItSystemUsage",
                column: "TechnicalSystemTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItSystemUsage_TechnicalSystemTypes_TechnicalSystemTypeId",
                table: "ItSystemUsage",
                column: "TechnicalSystemTypeId",
                principalTable: "TechnicalSystemTypes",
                principalColumn: "Id");
        }
    }
}
