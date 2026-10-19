using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddContractInvoiceSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceDates",
                schema: "dbo",
                table: "ItContractOverviewReadModels",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "InvoiceFrequency",
                schema: "dbo",
                table: "ItContractOverviewReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceFrequency",
                schema: "dbo",
                table: "ItContract",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ItContractInvoiceDates",
                schema: "dbo",
                columns: table => new
                {
                    ItContractId = table.Column<int>(type: "integer", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Day = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItContractInvoiceDates", x => new { x.ItContractId, x.Period });
                    table.ForeignKey(
                        name: "FK_ItContractInvoiceDates_ItContract_ItContractId",
                        column: x => x.ItContractId,
                        principalSchema: "dbo",
                        principalTable: "ItContract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItContractInvoiceDates",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "InvoiceDates",
                schema: "dbo",
                table: "ItContractOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "InvoiceFrequency",
                schema: "dbo",
                table: "ItContractOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "InvoiceFrequency",
                schema: "dbo",
                table: "ItContract");

        }
    }
}
