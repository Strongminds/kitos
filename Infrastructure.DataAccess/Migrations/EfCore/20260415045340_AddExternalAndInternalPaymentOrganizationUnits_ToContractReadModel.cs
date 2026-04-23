using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddExternalAndInternalPaymentOrganizationUnits_ToContractReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'ItContractOverviewReadModels'
          AND column_name = 'ExternalPaymentOrganizationUnitsCsv'
    ) THEN
        ALTER TABLE ""ItContractOverviewReadModels"" ADD COLUMN ""ExternalPaymentOrganizationUnitsCsv"" text;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'ItContractOverviewReadModels'
          AND column_name = 'InternalPaymentOrganizationUnitsCsv'
    ) THEN
        ALTER TABLE ""ItContractOverviewReadModels"" ADD COLUMN ""InternalPaymentOrganizationUnitsCsv"" text;
    END IF;
END $$;");

                return;
            }

            migrationBuilder.Sql(@"
IF COL_LENGTH('ItContractOverviewReadModels', 'ExternalPaymentOrganizationUnitsCsv') IS NULL
BEGIN
    ALTER TABLE [ItContractOverviewReadModels] ADD [ExternalPaymentOrganizationUnitsCsv] nvarchar(max) NULL;
END

IF COL_LENGTH('ItContractOverviewReadModels', 'InternalPaymentOrganizationUnitsCsv') IS NULL
BEGIN
    ALTER TABLE [ItContractOverviewReadModels] ADD [InternalPaymentOrganizationUnitsCsv] nvarchar(max) NULL;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'ItContractOverviewReadModels'
          AND column_name = 'ExternalPaymentOrganizationUnitsCsv'
    ) THEN
        ALTER TABLE ""ItContractOverviewReadModels"" DROP COLUMN ""ExternalPaymentOrganizationUnitsCsv"";
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'ItContractOverviewReadModels'
          AND column_name = 'InternalPaymentOrganizationUnitsCsv'
    ) THEN
        ALTER TABLE ""ItContractOverviewReadModels"" DROP COLUMN ""InternalPaymentOrganizationUnitsCsv"";
    END IF;
END $$;");

                return;
            }

            migrationBuilder.Sql(@"
IF COL_LENGTH('ItContractOverviewReadModels', 'ExternalPaymentOrganizationUnitsCsv') IS NOT NULL
BEGIN
    ALTER TABLE [ItContractOverviewReadModels] DROP COLUMN [ExternalPaymentOrganizationUnitsCsv];
END

IF COL_LENGTH('ItContractOverviewReadModels', 'InternalPaymentOrganizationUnitsCsv') IS NOT NULL
BEGIN
    ALTER TABLE [ItContractOverviewReadModels] DROP COLUMN [InternalPaymentOrganizationUnitsCsv];
END");
        }
    }
}
