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
            var isSqlServer = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";
            var maxTextType = isSqlServer ? "nvarchar(max)" : "text";
            var uuidType = isSqlServer ? "uniqueidentifier" : "uuid";
            var intType = isSqlServer ? "int" : "integer";
            var dateTimeType = isSqlServer ? "datetime2" : "timestamp without time zone";

            migrationBuilder.CreateTable(
                name: "ItSystemArchive",
                columns: table => new
                {
                    Id = isSqlServer
                        ? table.Column<int>(type: intType, nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1")
                        : table.Column<int>(type: intType, nullable: false)
                            .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: uuidType, nullable: false),
                    SnapshotUuid = table.Column<Guid>(type: uuidType, nullable: false),
                    OrganizationUuid = table.Column<Guid>(type: uuidType, nullable: false),
                    Note = table.Column<string>(type: maxTextType, nullable: false),
                    ArchivingDate = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    ReferenceName = table.Column<string>(type: maxTextType, nullable: false),
                    ObjectOwnerId = table.Column<int>(type: intType, nullable: false),
                    LastChanged = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    LastChangedByUserId = table.Column<int>(type: intType, nullable: false)
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
                    Id = isSqlServer
                        ? table.Column<int>(type: intType, nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1")
                        : table.Column<int>(type: intType, nullable: false)
                            .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: uuidType, nullable: false),
                    Label = table.Column<string>(type: maxTextType, nullable: false),
                    Url = table.Column<string>(type: maxTextType, nullable: false),
                    ItSystemArchiveUuid = table.Column<Guid>(type: uuidType, nullable: false),
                    ObjectOwnerId = table.Column<int>(type: intType, nullable: false),
                    LastChanged = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    LastChangedByUserId = table.Column<int>(type: intType, nullable: false)
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
                    Id = isSqlServer
                        ? table.Column<int>(type: intType, nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1")
                        : table.Column<int>(type: intType, nullable: false)
                            .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: uuidType, nullable: false),
                    LegacyName = table.Column<string>(type: maxTextType, nullable: true),
                    LocalName = table.Column<string>(type: maxTextType, nullable: true),
                    LocalId = table.Column<string>(type: maxTextType, nullable: true),
                    ItSystemArchiveUuid = table.Column<Guid>(type: uuidType, nullable: false),
                    ObjectOwnerId = table.Column<int>(type: intType, nullable: false),
                    LastChanged = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    LastChangedByUserId = table.Column<int>(type: intType, nullable: false)
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
                name: "Snapshot");

            migrationBuilder.DropTable(
                name: "ItSystemArchive");
        }
    }
}
