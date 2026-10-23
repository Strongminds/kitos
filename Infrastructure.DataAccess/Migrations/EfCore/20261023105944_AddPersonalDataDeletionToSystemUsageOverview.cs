using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddPersonalDataDeletionToSystemUsageOverview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonalDataDeletionCapability",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalDataDeletionDeadlineTypeName",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PersonalDataDeletionDeadlineTypeUuid",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonalDataDeletionMethod",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalDataDeletionProcedureDocumentationName",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalDataDeletionProcedureDocumentationUrl",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonalDataDeletionResponsibleUserId",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalDataDeletionResponsibleUserName",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PersonalDataDeletionResponsibleUserUuid",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalDataDeletionVerificationDocumentationName",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalDataDeletionVerificationDocumentationUrl",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PersonalDataDeletionVerifiedDate",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels",
                type: "timestamp without time zone",
                nullable: true);
            migrationBuilder.Sql("""
                UPDATE dbo."ItSystemUsageOverviewReadModels" AS overview
                SET "PersonalDataDeletionCapability" = usage."PersonalDataDeletionCapability",
                    "PersonalDataDeletionMethod" = usage."PersonalDataDeletionMethod",
                    "PersonalDataDeletionDeadlineTypeUuid" = deadline."Uuid",
                    "PersonalDataDeletionDeadlineTypeName" = deadline."Name",
                    "PersonalDataDeletionResponsibleUserId" = usage."PersonalDataDeletionResponsibleUserId",
                    "PersonalDataDeletionResponsibleUserUuid" = responsible."Uuid",
                    "PersonalDataDeletionResponsibleUserName" = CASE WHEN responsible."Id" IS NULL THEN NULL ELSE rtrim(concat(responsible."Name", ' ', responsible."LastName")) END,
                    "PersonalDataDeletionVerifiedDate" = usage."PersonalDataDeletionVerifiedDate",
                    "PersonalDataDeletionProcedureDocumentationName" = usage."PersonalDataDeletionProcedureDocumentationUrlName",
                    "PersonalDataDeletionProcedureDocumentationUrl" = usage."PersonalDataDeletionProcedureDocumentationUrl",
                    "PersonalDataDeletionVerificationDocumentationName" = usage."PersonalDataDeletionVerificationDocumentationUrlName",
                    "PersonalDataDeletionVerificationDocumentationUrl" = usage."PersonalDataDeletionVerificationDocumentationUrl"
                FROM dbo."ItSystemUsage" AS usage
                LEFT JOIN dbo."PersonalDataDeletionDeadlineTypes" AS deadline ON deadline."Id" = usage."PersonalDataDeletionDeadlineTypeId"
                LEFT JOIN dbo."User" AS responsible ON responsible."Id" = usage."PersonalDataDeletionResponsibleUserId"
                WHERE overview."SourceEntityId" = usage."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonalDataDeletionCapability",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "PersonalDataDeletionDeadlineTypeName",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "PersonalDataDeletionDeadlineTypeUuid",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "PersonalDataDeletionMethod",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "PersonalDataDeletionProcedureDocumentationName",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "PersonalDataDeletionProcedureDocumentationUrl",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "PersonalDataDeletionResponsibleUserId",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "PersonalDataDeletionResponsibleUserName",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "PersonalDataDeletionResponsibleUserUuid",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "PersonalDataDeletionVerificationDocumentationName",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "PersonalDataDeletionVerificationDocumentationUrl",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels");

            migrationBuilder.DropColumn(
                name: "PersonalDataDeletionVerifiedDate",
                schema: "dbo",
                table: "ItSystemUsageOverviewReadModels");

        }
    }
}
