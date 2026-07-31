using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddSupplierForeignCvrToSupplierOverviewReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var isSqlServer = migrationBuilder.ActiveProvider == InfrastructureConstants.SqlServerProviderName;
            var maxTextType = isSqlServer ? InfrastructureConstants.SqlServerMaxTextType : InfrastructureConstants.PostgreSqlMaxTextType;

            migrationBuilder.AddColumn<string>(
                name: "SupplierForeignCvr",
                table: "ItContractSupplierOverviewReadModels",
                type: maxTextType,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierForeignCvr",
                table: "ItContractSupplierOverviewReadModels");
        }
    }
}
