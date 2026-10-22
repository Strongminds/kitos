using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class StructureContractOverviewInvoiceDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rebuild from the source schedule instead of parsing localized display text.
            migrationBuilder.Sql("""
                UPDATE dbo."ItContractOverviewReadModels" AS overview
                SET "InvoiceDates" = COALESCE((
                    SELECT jsonb_agg(jsonb_build_object('Month', date."Month", 'Day', date."Day") ORDER BY date."Period")::text
                    FROM dbo."ItContractInvoiceDates" AS date
                    WHERE date."ItContractId" = overview."SourceEntityId"
                ), '[]');
                ALTER TABLE dbo."ItContractOverviewReadModels"
                    ALTER COLUMN "InvoiceDates" DROP DEFAULT,
                    ALTER COLUMN "InvoiceDates" DROP NOT NULL,
                    ALTER COLUMN "InvoiceDates" TYPE jsonb USING "InvoiceDates"::jsonb;
                """);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceDate1Day",
                schema: "dbo",
                table: "ItContractOverviewReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceDate1Month",
                schema: "dbo",
                table: "ItContractOverviewReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceDate2Day",
                schema: "dbo",
                table: "ItContractOverviewReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceDate2Month",
                schema: "dbo",
                table: "ItContractOverviewReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceDate3Day",
                schema: "dbo",
                table: "ItContractOverviewReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceDate3Month",
                schema: "dbo",
                table: "ItContractOverviewReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceDate4Day",
                schema: "dbo",
                table: "ItContractOverviewReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceDate4Month",
                schema: "dbo",
                table: "ItContractOverviewReadModels",
                type: "integer",
                nullable: true);

            // Populate existing overviews using domain period identifiers, including sparse schedules.
            for (var period = 1; period <= 4; period++)
            {
                migrationBuilder.Sql($"""
                    UPDATE dbo."ItContractOverviewReadModels" AS overview
                    SET ("InvoiceDate{period}Month", "InvoiceDate{period}Day") = (
                        SELECT date."Month", date."Day"
                        FROM dbo."ItContractInvoiceDates" AS date
                        WHERE date."ItContractId" = overview."SourceEntityId" AND date."Period" = {period}
                    );
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceDate1Day",
                schema: "dbo",
                table: "ItContractOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "InvoiceDate1Month",
                schema: "dbo",
                table: "ItContractOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "InvoiceDate2Day",
                schema: "dbo",
                table: "ItContractOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "InvoiceDate2Month",
                schema: "dbo",
                table: "ItContractOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "InvoiceDate3Day",
                schema: "dbo",
                table: "ItContractOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "InvoiceDate3Month",
                schema: "dbo",
                table: "ItContractOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "InvoiceDate4Day",
                schema: "dbo",
                table: "ItContractOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "InvoiceDate4Month",
                schema: "dbo",
                table: "ItContractOverviewReadModels");


            migrationBuilder.Sql("""
                ALTER TABLE dbo."ItContractOverviewReadModels"
                    ALTER COLUMN "InvoiceDates" TYPE text USING "InvoiceDates"::text;
                UPDATE dbo."ItContractOverviewReadModels" AS overview
                SET "InvoiceDates" = COALESCE((
                    SELECT string_agg(
                        CASE overview."InvoiceFrequency"
                            WHEN 0 THEN 'Årligt'
                            WHEN 1 THEN date."Period"::text || '. halvår'
                            WHEN 2 THEN date."Period"::text || '. kvartal'
                        END || ': ' || lpad(date."Day"::text, 2, '0') || '-' || lpad(date."Month"::text, 2, '0'),
                        '; ' ORDER BY date."Period")
                    FROM dbo."ItContractInvoiceDates" AS date
                    WHERE date."ItContractId" = overview."SourceEntityId"
                ), '');
                ALTER TABLE dbo."ItContractOverviewReadModels"
                    ALTER COLUMN "InvoiceDates" SET DEFAULT '',
                    ALTER COLUMN "InvoiceDates" SET NOT NULL;
                """);
        }
    }
}
