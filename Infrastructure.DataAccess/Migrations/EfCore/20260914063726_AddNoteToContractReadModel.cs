using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddNoteToContractReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "dbo",
                table: "ItContractOverviewReadModels",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Note",
                schema: "dbo",
                table: "ItContractOverviewReadModels",
                column: "Note");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Note",
                schema: "dbo",
                table: "ItContractOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "dbo",
                table: "ItContractOverviewReadModels");
        }
    }
}
