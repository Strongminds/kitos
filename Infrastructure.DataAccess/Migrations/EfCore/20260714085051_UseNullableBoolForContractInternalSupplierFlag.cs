using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class UseNullableBoolForContractInternalSupplierFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItContract_Supplier_Read_IsInternalContract",
                table: "ItContractSupplierOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "IsInternalContract",
                table: "ItContractSupplierOverviewReadModels");

            migrationBuilder.AlterColumn<string>(
                name: "SupplierCvr",
                table: "ItContractSupplierOverviewReadModels",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierType",
                table: "ItContractSupplierOverviewReadModels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasInternalSupplier",
                table: "ItContract",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItContract_Supplier_Read_SupplierType",
                table: "ItContractSupplierOverviewReadModels",
                column: "SupplierType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItContract_Supplier_Read_SupplierType",
                table: "ItContractSupplierOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "SupplierType",
                table: "ItContractSupplierOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "HasInternalSupplier",
                table: "ItContract");

            migrationBuilder.AlterColumn<string>(
                name: "SupplierCvr",
                table: "ItContractSupplierOverviewReadModels",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInternalContract",
                table: "ItContractSupplierOverviewReadModels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ItContract_Supplier_Read_IsInternalContract",
                table: "ItContractSupplierOverviewReadModels",
                column: "IsInternalContract");
        }
    }
}
