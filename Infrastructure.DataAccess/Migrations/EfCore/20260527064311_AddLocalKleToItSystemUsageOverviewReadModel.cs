using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddLocalKleToItSystemUsageOverviewReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var isSqlServer = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";

            var maxTextType = isSqlServer ? "nvarchar(max)" : "text";
            var varchar15Type = isSqlServer ? "nvarchar(15)" : "character varying(15)";
            var varchar150Type = isSqlServer ? "nvarchar(150)" : "character varying(150)";
            var intType = isSqlServer ? "int" : "integer";

            migrationBuilder.AddColumn<string>(
                name: "LocalKleIdsAsCsv",
                table: "ItSystemUsageOverviewReadModels",
                type: maxTextType,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocalKleNamesAsCsv",
                table: "ItSystemUsageOverviewReadModels",
                type: maxTextType,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ItSystemUsageOverviewLocalTaskRefReadModels",
                columns: table => new
                {
                    Id = isSqlServer
                        ? table.Column<int>(type: intType, nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1")
                        : table.Column<int>(type: "serial", nullable: false),
                    KLEId = table.Column<string>(type: varchar15Type, maxLength: 15, nullable: true),
                    KLEName = table.Column<string>(type: varchar150Type, maxLength: 150, nullable: true),
                    ParentId = table.Column<int>(type: intType, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItSystemUsageOverviewLocalTaskRefReadModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItSystemUsageOverviewLocalTaskRefReadModels_ItSystemUsageOverviewReadModels_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ItSystemUsageOverviewReadModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ItSystemUsageOverviewLocalTaskRefReadModel_Index_KLEId",
                table: "ItSystemUsageOverviewLocalTaskRefReadModels",
                column: "KLEId");

            migrationBuilder.CreateIndex(
                name: "ItSystemUsageOverviewLocalTaskRefReadModel_Index_KLEName",
                table: "ItSystemUsageOverviewLocalTaskRefReadModels",
                column: "KLEName");

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemUsageOverviewLocalTaskRefReadModels_ParentId",
                table: "ItSystemUsageOverviewLocalTaskRefReadModels",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItSystemUsageOverviewLocalTaskRefReadModels");

            migrationBuilder.DropColumn(
                name: "LocalKleIdsAsCsv",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "LocalKleNamesAsCsv",
                table: "ItSystemUsageOverviewReadModels");
        }
    }
}
