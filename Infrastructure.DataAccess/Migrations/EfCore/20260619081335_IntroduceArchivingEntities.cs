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
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Organization_Uuid",
                table: "Organization",
                column: "Uuid");

            migrationBuilder.CreateTable(
                name: "ItSystemArchive",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItSystemUsageSnapshotUuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                name: "ItSystemUsageArchiveSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegacyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItSystemArchiveUuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectOwnerId = table.Column<int>(type: "int", nullable: false),
                    LastChanged = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastChangedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItSystemUsageArchiveSnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItSystemUsageArchiveSnapshot_ItSystemArchive_ItSystemArchiveUuid",
                        column: x => x.ItSystemArchiveUuid,
                        principalTable: "ItSystemArchive",
                        principalColumn: "Uuid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItSystemUsageArchiveSnapshot_User_LastChangedByUserId",
                        column: x => x.LastChangedByUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItSystemUsageArchiveSnapshot_User_ObjectOwnerId",
                        column: x => x.ObjectOwnerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                column: "ItSystemUsageSnapshotUuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ItSystemArchive_Uuid",
                table: "ItSystemArchive",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemUsageArchiveSnapshot_LastChangedByUserId",
                table: "ItSystemUsageArchiveSnapshot",
                column: "LastChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemUsageArchiveSnapshot_ObjectOwnerId",
                table: "ItSystemUsageArchiveSnapshot",
                column: "ObjectOwnerId");

            migrationBuilder.CreateIndex(
                name: "UX_ItSystemUsageArchiveSnapshot_ItSystemArchiveUuid",
                table: "ItSystemUsageArchiveSnapshot",
                column: "ItSystemArchiveUuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ItSystemUsageArchiveSnapshot_Uuid",
                table: "ItSystemUsageArchiveSnapshot",
                column: "Uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchiveReference");

            migrationBuilder.DropTable(
                name: "ItSystemUsageArchiveSnapshot");

            migrationBuilder.DropTable(
                name: "ItSystemArchive");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Organization_Uuid",
                table: "Organization");
        }
    }
}
