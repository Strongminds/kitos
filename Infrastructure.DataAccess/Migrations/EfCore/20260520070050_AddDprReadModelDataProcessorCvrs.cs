using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddDprReadModelDataProcessorCvrs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var isSqlServer = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";
            var maxTextType = isSqlServer ? "nvarchar(max)" : "text";

            migrationBuilder.AddColumn<string>(
                name: "DataProcessorCvrsAsCsv",
                table: "DataProcessingRegistrationReadModels",
                type: maxTextType,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubDataProcessorCvrsAsCsv",
                table: "DataProcessingRegistrationReadModels",
                type: maxTextType,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataProcessorCvrsAsCsv",
                table: "DataProcessingRegistrationReadModels");

            migrationBuilder.DropColumn(
                name: "SubDataProcessorCvrsAsCsv",
                table: "DataProcessingRegistrationReadModels");
        }
    }
}
