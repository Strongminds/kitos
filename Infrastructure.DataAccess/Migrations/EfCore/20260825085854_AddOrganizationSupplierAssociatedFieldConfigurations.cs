using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddOrganizationSupplierAssociatedFieldConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskRefItSystemUsageOptOut_ItSystemUsage_ItSystemUsage_Id",
                table: "TaskRefItSystemUsageOptOut");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskRefItSystemUsageOptOut_TaskRef_TaskRef_Id",
                table: "TaskRefItSystemUsageOptOut");

            migrationBuilder.DropIndex(
                name: "UX_TaskKey",
                table: "TaskRef");

            migrationBuilder.DropIndex(
                name: "IX_Organization_ContactPerson_Id",
                table: "Organization");

            migrationBuilder.DropIndex(
                name: "IX_ItSystemUsageOrgUnitUsages_ResponsibleItSystemUsage_Id",
                table: "ItSystemUsageOrgUnitUsages");

            migrationBuilder.DropIndex(
                name: "IX_ItContractItSystemUsages_ItSystemUsage_Id",
                table: "ItContractItSystemUsages");

            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.RenameTable(
                name: "UserNotifications",
                newName: "UserNotifications",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "User",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "UIModuleCustomizations",
                newName: "UIModuleCustomizations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Text",
                newName: "Text",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "TerminationDeadlineTypes",
                newName: "TerminationDeadlineTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "TechnicalSystemTypes",
                newName: "TechnicalSystemTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "TaskRefItSystemUsages",
                newName: "TaskRefItSystemUsages",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "TaskRefItSystemUsageOptOut",
                newName: "TaskRefItSystemUsageOptOut",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "TaskRefItSystems",
                newName: "TaskRefItSystems",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "TaskRef",
                newName: "TaskRef",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "SystemUsageCriticalityLevelTypes",
                newName: "SystemUsageCriticalityLevelTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "SystemRelations",
                newName: "SystemRelations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "SubDataProcessors",
                newName: "SubDataProcessors",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "StsOrganizationIdentities",
                newName: "StsOrganizationIdentities",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "StsOrganizationConsequenceLogs",
                newName: "StsOrganizationConsequenceLogs",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "StsOrganizationConnections",
                newName: "StsOrganizationConnections",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "StsOrganizationChangeLogs",
                newName: "StsOrganizationChangeLogs",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "SsoUserIdentities",
                newName: "SsoUserIdentities",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Snapshot",
                newName: "Snapshot",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "SensitivePersonalDataTypes",
                newName: "SensitivePersonalDataTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "SensitiveDataTypes",
                newName: "SensitiveDataTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "RelationFrequencyTypes",
                newName: "RelationFrequencyTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "RegisterTypes",
                newName: "RegisterTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "PurchaseFormTypes",
                newName: "PurchaseFormTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "PublicMessages",
                newName: "PublicMessages",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ProcurementStrategyTypes",
                newName: "ProcurementStrategyTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "PriceRegulationTypes",
                newName: "PriceRegulationTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "PendingReadModelUpdates",
                newName: "PendingReadModelUpdates",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "PaymentModelTypes",
                newName: "PaymentModelTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "PaymentFreqencyTypes",
                newName: "PaymentFreqencyTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "PasswordResetRequest",
                newName: "PasswordResetRequest",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "OrganizationUnitRoles",
                newName: "OrganizationUnitRoles",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "OrganizationUnitRights",
                newName: "OrganizationUnitRights",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "OrganizationUnit",
                newName: "OrganizationUnit",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "OrganizationTypes",
                newName: "OrganizationTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "OrganizationSuppliers",
                newName: "OrganizationSuppliers",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "OrganizationRights",
                newName: "OrganizationRights",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Organization",
                newName: "Organization",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "OptionExtendTypes",
                newName: "OptionExtendTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalTerminationDeadlineTypes",
                newName: "LocalTerminationDeadlineTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalTechnicalSystemTypes",
                newName: "LocalTechnicalSystemTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalSystemUsageCriticalityLevelTypes",
                newName: "LocalSystemUsageCriticalityLevelTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalSensitivePersonalDataTypes",
                newName: "LocalSensitivePersonalDataTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalSensitiveDataTypes",
                newName: "LocalSensitiveDataTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalRelationFrequencyTypes",
                newName: "LocalRelationFrequencyTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalRegisterTypes",
                newName: "LocalRegisterTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalPurchaseFormTypes",
                newName: "LocalPurchaseFormTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalProcurementStrategyTypes",
                newName: "LocalProcurementStrategyTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalPriceRegulationTypes",
                newName: "LocalPriceRegulationTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalPaymentModelTypes",
                newName: "LocalPaymentModelTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalPaymentFreqencyTypes",
                newName: "LocalPaymentFreqencyTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalOrganizationUnitRoles",
                newName: "LocalOrganizationUnitRoles",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalOptionExtendTypes",
                newName: "LocalOptionExtendTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalItSystemRoles",
                newName: "LocalItSystemRoles",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalItSystemCategories",
                newName: "LocalItSystemCategories",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalItContractTypes",
                newName: "LocalItContractTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalItContractTemplateTypes",
                newName: "LocalItContractTemplateTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalItContractRoles",
                newName: "LocalItContractRoles",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalInterfaceTypes",
                newName: "LocalInterfaceTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalDataTypes",
                newName: "LocalDataTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalDataProcessingRegistrationRoles",
                newName: "LocalDataProcessingRegistrationRoles",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalDataProcessingOversightOptions",
                newName: "LocalDataProcessingOversightOptions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalDataProcessingDataResponsibleOptions",
                newName: "LocalDataProcessingDataResponsibleOptions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalDataProcessingCountryOptions",
                newName: "LocalDataProcessingCountryOptions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalDataProcessingBasisForTransferOptions",
                newName: "LocalDataProcessingBasisForTransferOptions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalCriticalityTypes",
                newName: "LocalCriticalityTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalBusinessTypes",
                newName: "LocalBusinessTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalArchiveTypes",
                newName: "LocalArchiveTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalArchiveTestLocations",
                newName: "LocalArchiveTestLocations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalArchiveLocations",
                newName: "LocalArchiveLocations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LocalAgreementElementTypes",
                newName: "LocalAgreementElementTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LifeCycleTrackingEvents",
                newName: "LifeCycleTrackingEvents",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "KLEUpdateHistoryItems",
                newName: "KLEUpdateHistoryItems",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "KendoOrganizationalConfigurations",
                newName: "KendoOrganizationalConfigurations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "KendoColumnConfigurations",
                newName: "KendoColumnConfigurations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageTechnicalSystemTypes",
                newName: "ItSystemUsageTechnicalSystemTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageSensitiveDataLevels",
                newName: "ItSystemUsageSensitiveDataLevels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsagePersonalDatas",
                newName: "ItSystemUsagePersonalDatas",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewUsingSystemUsageReadModels",
                newName: "ItSystemUsageOverviewUsingSystemUsageReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewUsedBySystemUsageReadModels",
                newName: "ItSystemUsageOverviewUsedBySystemUsageReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewTechnicalSystemTypeReadModel",
                newName: "ItSystemUsageOverviewTechnicalSystemTypeReadModel",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewTaskRefReadModels",
                newName: "ItSystemUsageOverviewTaskRefReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewSensitiveDataLevelReadModels",
                newName: "ItSystemUsageOverviewSensitiveDataLevelReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewRoleAssignmentReadModels",
                newName: "ItSystemUsageOverviewRoleAssignmentReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewRelevantOrgUnitReadModels",
                newName: "ItSystemUsageOverviewRelevantOrgUnitReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewReadModels",
                newName: "ItSystemUsageOverviewReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewLocalTaskRefReadModels",
                newName: "ItSystemUsageOverviewLocalTaskRefReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewItContractReadModels",
                newName: "ItSystemUsageOverviewItContractReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewInterfaceReadModels",
                newName: "ItSystemUsageOverviewInterfaceReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewDataProcessingRegistrationReadModels",
                newName: "ItSystemUsageOverviewDataProcessingRegistrationReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewArchivePeriodReadModels",
                newName: "ItSystemUsageOverviewArchivePeriodReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOrgUnitUsages",
                newName: "ItSystemUsageOrgUnitUsages",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageArchive",
                newName: "ItSystemUsageArchive",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemUsage",
                newName: "ItSystemUsage",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemRoles",
                newName: "ItSystemRoles",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemRights",
                newName: "ItSystemRights",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystemCategories",
                newName: "ItSystemCategories",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItSystem",
                newName: "ItSystem",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItInterface",
                newName: "ItInterface",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContractTypes",
                newName: "ItContractTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContractTemplateTypes",
                newName: "ItContractTemplateTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContractSupplierOverviewReadModels",
                newName: "ItContractSupplierOverviewReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContractSupplierOverviewAtCriticalityContractReadModels",
                newName: "ItContractSupplierOverviewAtCriticalityContractReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContractRoles",
                newName: "ItContractRoles",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContractRights",
                newName: "ItContractRights",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContractOverviewRoleAssignmentReadModels",
                newName: "ItContractOverviewRoleAssignmentReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContractOverviewReadModelSystemRelations",
                newName: "ItContractOverviewReadModelSystemRelations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContractOverviewReadModels",
                newName: "ItContractOverviewReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContractOverviewReadModelItSystemUsages",
                newName: "ItContractOverviewReadModelItSystemUsages",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContractOverviewReadModelDataProcessingAgreements",
                newName: "ItContractOverviewReadModelDataProcessingAgreements",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContractItSystemUsages",
                newName: "ItContractItSystemUsages",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContractDataProcessingRegistrations",
                newName: "ItContractDataProcessingRegistrations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContractAgreementElementTypes",
                newName: "ItContractAgreementElementTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ItContract",
                newName: "ItContract",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "InterfaceTypes",
                newName: "InterfaceTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "HelpTexts",
                newName: "HelpTexts",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ExternalReferences",
                newName: "ExternalReferences",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Exhibit",
                newName: "Exhibit",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "EconomyStream",
                newName: "EconomyStream",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataTypes",
                newName: "DataTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataRow",
                newName: "DataRow",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataResponsibles",
                newName: "DataResponsibles",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProtectionAdvisors",
                newName: "DataProtectionAdvisors",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrations",
                newName: "DataProcessingRegistrations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationRoles",
                newName: "DataProcessingRegistrationRoles",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationRoleAssignmentReadModels",
                newName: "DataProcessingRegistrationRoleAssignmentReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationRights",
                newName: "DataProcessingRegistrationRights",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationReadModels",
                newName: "DataProcessingRegistrationReadModels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationOversightDates",
                newName: "DataProcessingRegistrationOversightDates",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationOrganizations",
                newName: "DataProcessingRegistrationOrganizations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationItSystemUsages",
                newName: "DataProcessingRegistrationItSystemUsages",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationDataProcessingOversightOptions",
                newName: "DataProcessingRegistrationDataProcessingOversightOptions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationDataProcessingCountryOptions",
                newName: "DataProcessingRegistrationDataProcessingCountryOptions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProcessingOversightOptions",
                newName: "DataProcessingOversightOptions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProcessingDataResponsibleOptions",
                newName: "DataProcessingDataResponsibleOptions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProcessingCountryOptions",
                newName: "DataProcessingCountryOptions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DataProcessingBasisForTransferOptions",
                newName: "DataProcessingBasisForTransferOptions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "CustomizedUiNodes",
                newName: "CustomizedUiNodes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "CriticalityTypes",
                newName: "CriticalityTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "CountryCodes",
                newName: "CountryCodes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ContactPersons",
                newName: "ContactPersons",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Config",
                newName: "Config",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "BusinessTypes",
                newName: "BusinessTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "BrokenLinkInInterfaces",
                newName: "BrokenLinkInInterfaces",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "BrokenLinkInExternalReferences",
                newName: "BrokenLinkInExternalReferences",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "BrokenExternalReferencesReports",
                newName: "BrokenExternalReferencesReports",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "AttachedOptions",
                newName: "AttachedOptions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ArchiveTypes",
                newName: "ArchiveTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ArchiveTestLocations",
                newName: "ArchiveTestLocations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ArchiveReference",
                newName: "ArchiveReference",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ArchivePeriod",
                newName: "ArchivePeriod",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ArchiveLocations",
                newName: "ArchiveLocations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "AgreementElementTypes",
                newName: "AgreementElementTypes",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "AdviceUserRelations",
                newName: "AdviceUserRelations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "AdviceSents",
                newName: "AdviceSents",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Advice",
                newName: "Advice",
                newSchema: "dbo");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "TerminationDeadlineTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "TechnicalSystemTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "SystemUsageCriticalityLevelTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "SensitivePersonalDataTypes",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "SensitiveDataTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "RelationFrequencyTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "RegisterTypes",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "PurchaseFormTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "ProcurementStrategyTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "PriceRegulationTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "PaymentModelTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "PaymentFreqencyTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "OrganizationUnitRoles",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "OrganizationUnit",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "Organization",
                type: "citext",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "OptionExtendTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "ItSystemRoles",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "ItSystemCategories",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "ItSystem",
                type: "citext",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "ItInterface",
                type: "citext",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "ItContractTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "ItContractTemplateTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "ItContractRoles",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "ItContract",
                type: "citext",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "InterfaceTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "DataTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "DataProcessingRegistrations",
                type: "citext",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "DataProcessingRegistrationRoles",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "DataProcessingOversightOptions",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "DataProcessingDataResponsibleOptions",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "DataProcessingCountryOptions",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "DataProcessingBasisForTransferOptions",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "CriticalityTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "CountryCodes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "BusinessTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "ArchiveTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "ArchiveTestLocations",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "ArchiveLocations",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "dbo",
                table: "AgreementElementTypes",
                type: "citext",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.CreateTable(
                name: "SupplierAssociatedFieldConfiguration",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FieldKey = table.Column<string>(type: "text", nullable: true),
                    ControlState = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    ObjectOwnerId = table.Column<int>(type: "integer", nullable: true),
                    LastChanged = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastChangedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierAssociatedFieldConfiguration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierAssociatedFieldConfiguration_Organization_Organizat~",
                        column: x => x.OrganizationId,
                        principalSchema: "dbo",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierAssociatedFieldConfiguration_User_LastChangedByUser~",
                        column: x => x.LastChangedByUserId,
                        principalSchema: "dbo",
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplierAssociatedFieldConfiguration_User_ObjectOwnerId",
                        column: x => x.ObjectOwnerId,
                        principalSchema: "dbo",
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "UX_TaskKey",
                schema: "dbo",
                table: "TaskRef",
                column: "TaskKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organization_ContactPerson_Id",
                schema: "dbo",
                table: "Organization",
                column: "ContactPerson_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemUsageOrgUnitUsages_ResponsibleItSystemUsage_Id",
                schema: "dbo",
                table: "ItSystemUsageOrgUnitUsages",
                column: "ResponsibleItSystemUsage_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItContractItSystemUsages_ItSystemUsage_Id",
                schema: "dbo",
                table: "ItContractItSystemUsages",
                column: "ItSystemUsage_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAssociatedFieldConfiguration_LastChangedByUserId",
                schema: "dbo",
                table: "SupplierAssociatedFieldConfiguration",
                column: "LastChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAssociatedFieldConfiguration_ObjectOwnerId",
                schema: "dbo",
                table: "SupplierAssociatedFieldConfiguration",
                column: "ObjectOwnerId");

            migrationBuilder.CreateIndex(
                name: "UX_OrganizationId_SupplierAssociatedFieldConfiguration_FieldKey",
                schema: "dbo",
                table: "SupplierAssociatedFieldConfiguration",
                columns: new[] { "OrganizationId", "FieldKey" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DataProcessingRegistrationDataProcessingCountryOptions_Dat~1",
                schema: "dbo",
                table: "DataProcessingRegistrationDataProcessingCountryOptions",
                column: "DataProcessingRegistration_Id",
                principalSchema: "dbo",
                principalTable: "DataProcessingRegistrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DataProcessingRegistrationDataProcessingOversightOptions_D~1",
                schema: "dbo",
                table: "DataProcessingRegistrationDataProcessingOversightOptions",
                column: "DataProcessingRegistration_Id",
                principalSchema: "dbo",
                principalTable: "DataProcessingRegistrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DataProcessingRegistrationRights_DataProcessingRegistratio~1",
                schema: "dbo",
                table: "DataProcessingRegistrationRights",
                column: "RoleId",
                principalSchema: "dbo",
                principalTable: "DataProcessingRegistrationRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskRefItSystemUsageOptOut_ItSystemUsage_ItSystemUsage_Id",
                schema: "dbo",
                table: "TaskRefItSystemUsageOptOut",
                column: "ItSystemUsage_Id",
                principalSchema: "dbo",
                principalTable: "ItSystemUsage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskRefItSystemUsageOptOut_TaskRef_TaskRef_Id",
                schema: "dbo",
                table: "TaskRefItSystemUsageOptOut",
                column: "TaskRef_Id",
                principalSchema: "dbo",
                principalTable: "TaskRef",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataProcessingRegistrationDataProcessingCountryOptions_Dat~1",
                schema: "dbo",
                table: "DataProcessingRegistrationDataProcessingCountryOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_DataProcessingRegistrationDataProcessingOversightOptions_D~1",
                schema: "dbo",
                table: "DataProcessingRegistrationDataProcessingOversightOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_DataProcessingRegistrationRights_DataProcessingRegistratio~1",
                schema: "dbo",
                table: "DataProcessingRegistrationRights");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskRefItSystemUsageOptOut_ItSystemUsage_ItSystemUsage_Id",
                schema: "dbo",
                table: "TaskRefItSystemUsageOptOut");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskRefItSystemUsageOptOut_TaskRef_TaskRef_Id",
                schema: "dbo",
                table: "TaskRefItSystemUsageOptOut");

            migrationBuilder.DropTable(
                name: "SupplierAssociatedFieldConfiguration",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "UX_TaskKey",
                schema: "dbo",
                table: "TaskRef");

            migrationBuilder.DropIndex(
                name: "IX_Organization_ContactPerson_Id",
                schema: "dbo",
                table: "Organization");

            migrationBuilder.DropIndex(
                name: "IX_ItSystemUsageOrgUnitUsages_ResponsibleItSystemUsage_Id",
                schema: "dbo",
                table: "ItSystemUsageOrgUnitUsages");

            migrationBuilder.DropIndex(
                name: "IX_ItContractItSystemUsages_ItSystemUsage_Id",
                schema: "dbo",
                table: "ItContractItSystemUsages");

            migrationBuilder.RenameTable(
                name: "UserNotifications",
                schema: "dbo",
                newName: "UserNotifications");

            migrationBuilder.RenameTable(
                name: "User",
                schema: "dbo",
                newName: "User");

            migrationBuilder.RenameTable(
                name: "UIModuleCustomizations",
                schema: "dbo",
                newName: "UIModuleCustomizations");

            migrationBuilder.RenameTable(
                name: "Text",
                schema: "dbo",
                newName: "Text");

            migrationBuilder.RenameTable(
                name: "TerminationDeadlineTypes",
                schema: "dbo",
                newName: "TerminationDeadlineTypes");

            migrationBuilder.RenameTable(
                name: "TechnicalSystemTypes",
                schema: "dbo",
                newName: "TechnicalSystemTypes");

            migrationBuilder.RenameTable(
                name: "TaskRefItSystemUsages",
                schema: "dbo",
                newName: "TaskRefItSystemUsages");

            migrationBuilder.RenameTable(
                name: "TaskRefItSystemUsageOptOut",
                schema: "dbo",
                newName: "TaskRefItSystemUsageOptOut");

            migrationBuilder.RenameTable(
                name: "TaskRefItSystems",
                schema: "dbo",
                newName: "TaskRefItSystems");

            migrationBuilder.RenameTable(
                name: "TaskRef",
                schema: "dbo",
                newName: "TaskRef");

            migrationBuilder.RenameTable(
                name: "SystemUsageCriticalityLevelTypes",
                schema: "dbo",
                newName: "SystemUsageCriticalityLevelTypes");

            migrationBuilder.RenameTable(
                name: "SystemRelations",
                schema: "dbo",
                newName: "SystemRelations");

            migrationBuilder.RenameTable(
                name: "SubDataProcessors",
                schema: "dbo",
                newName: "SubDataProcessors");

            migrationBuilder.RenameTable(
                name: "StsOrganizationIdentities",
                schema: "dbo",
                newName: "StsOrganizationIdentities");

            migrationBuilder.RenameTable(
                name: "StsOrganizationConsequenceLogs",
                schema: "dbo",
                newName: "StsOrganizationConsequenceLogs");

            migrationBuilder.RenameTable(
                name: "StsOrganizationConnections",
                schema: "dbo",
                newName: "StsOrganizationConnections");

            migrationBuilder.RenameTable(
                name: "StsOrganizationChangeLogs",
                schema: "dbo",
                newName: "StsOrganizationChangeLogs");

            migrationBuilder.RenameTable(
                name: "SsoUserIdentities",
                schema: "dbo",
                newName: "SsoUserIdentities");

            migrationBuilder.RenameTable(
                name: "Snapshot",
                schema: "dbo",
                newName: "Snapshot");

            migrationBuilder.RenameTable(
                name: "SensitivePersonalDataTypes",
                schema: "dbo",
                newName: "SensitivePersonalDataTypes");

            migrationBuilder.RenameTable(
                name: "SensitiveDataTypes",
                schema: "dbo",
                newName: "SensitiveDataTypes");

            migrationBuilder.RenameTable(
                name: "RelationFrequencyTypes",
                schema: "dbo",
                newName: "RelationFrequencyTypes");

            migrationBuilder.RenameTable(
                name: "RegisterTypes",
                schema: "dbo",
                newName: "RegisterTypes");

            migrationBuilder.RenameTable(
                name: "PurchaseFormTypes",
                schema: "dbo",
                newName: "PurchaseFormTypes");

            migrationBuilder.RenameTable(
                name: "PublicMessages",
                schema: "dbo",
                newName: "PublicMessages");

            migrationBuilder.RenameTable(
                name: "ProcurementStrategyTypes",
                schema: "dbo",
                newName: "ProcurementStrategyTypes");

            migrationBuilder.RenameTable(
                name: "PriceRegulationTypes",
                schema: "dbo",
                newName: "PriceRegulationTypes");

            migrationBuilder.RenameTable(
                name: "PendingReadModelUpdates",
                schema: "dbo",
                newName: "PendingReadModelUpdates");

            migrationBuilder.RenameTable(
                name: "PaymentModelTypes",
                schema: "dbo",
                newName: "PaymentModelTypes");

            migrationBuilder.RenameTable(
                name: "PaymentFreqencyTypes",
                schema: "dbo",
                newName: "PaymentFreqencyTypes");

            migrationBuilder.RenameTable(
                name: "PasswordResetRequest",
                schema: "dbo",
                newName: "PasswordResetRequest");

            migrationBuilder.RenameTable(
                name: "OrganizationUnitRoles",
                schema: "dbo",
                newName: "OrganizationUnitRoles");

            migrationBuilder.RenameTable(
                name: "OrganizationUnitRights",
                schema: "dbo",
                newName: "OrganizationUnitRights");

            migrationBuilder.RenameTable(
                name: "OrganizationUnit",
                schema: "dbo",
                newName: "OrganizationUnit");

            migrationBuilder.RenameTable(
                name: "OrganizationTypes",
                schema: "dbo",
                newName: "OrganizationTypes");

            migrationBuilder.RenameTable(
                name: "OrganizationSuppliers",
                schema: "dbo",
                newName: "OrganizationSuppliers");

            migrationBuilder.RenameTable(
                name: "OrganizationRights",
                schema: "dbo",
                newName: "OrganizationRights");

            migrationBuilder.RenameTable(
                name: "Organization",
                schema: "dbo",
                newName: "Organization");

            migrationBuilder.RenameTable(
                name: "OptionExtendTypes",
                schema: "dbo",
                newName: "OptionExtendTypes");

            migrationBuilder.RenameTable(
                name: "LocalTerminationDeadlineTypes",
                schema: "dbo",
                newName: "LocalTerminationDeadlineTypes");

            migrationBuilder.RenameTable(
                name: "LocalTechnicalSystemTypes",
                schema: "dbo",
                newName: "LocalTechnicalSystemTypes");

            migrationBuilder.RenameTable(
                name: "LocalSystemUsageCriticalityLevelTypes",
                schema: "dbo",
                newName: "LocalSystemUsageCriticalityLevelTypes");

            migrationBuilder.RenameTable(
                name: "LocalSensitivePersonalDataTypes",
                schema: "dbo",
                newName: "LocalSensitivePersonalDataTypes");

            migrationBuilder.RenameTable(
                name: "LocalSensitiveDataTypes",
                schema: "dbo",
                newName: "LocalSensitiveDataTypes");

            migrationBuilder.RenameTable(
                name: "LocalRelationFrequencyTypes",
                schema: "dbo",
                newName: "LocalRelationFrequencyTypes");

            migrationBuilder.RenameTable(
                name: "LocalRegisterTypes",
                schema: "dbo",
                newName: "LocalRegisterTypes");

            migrationBuilder.RenameTable(
                name: "LocalPurchaseFormTypes",
                schema: "dbo",
                newName: "LocalPurchaseFormTypes");

            migrationBuilder.RenameTable(
                name: "LocalProcurementStrategyTypes",
                schema: "dbo",
                newName: "LocalProcurementStrategyTypes");

            migrationBuilder.RenameTable(
                name: "LocalPriceRegulationTypes",
                schema: "dbo",
                newName: "LocalPriceRegulationTypes");

            migrationBuilder.RenameTable(
                name: "LocalPaymentModelTypes",
                schema: "dbo",
                newName: "LocalPaymentModelTypes");

            migrationBuilder.RenameTable(
                name: "LocalPaymentFreqencyTypes",
                schema: "dbo",
                newName: "LocalPaymentFreqencyTypes");

            migrationBuilder.RenameTable(
                name: "LocalOrganizationUnitRoles",
                schema: "dbo",
                newName: "LocalOrganizationUnitRoles");

            migrationBuilder.RenameTable(
                name: "LocalOptionExtendTypes",
                schema: "dbo",
                newName: "LocalOptionExtendTypes");

            migrationBuilder.RenameTable(
                name: "LocalItSystemRoles",
                schema: "dbo",
                newName: "LocalItSystemRoles");

            migrationBuilder.RenameTable(
                name: "LocalItSystemCategories",
                schema: "dbo",
                newName: "LocalItSystemCategories");

            migrationBuilder.RenameTable(
                name: "LocalItContractTypes",
                schema: "dbo",
                newName: "LocalItContractTypes");

            migrationBuilder.RenameTable(
                name: "LocalItContractTemplateTypes",
                schema: "dbo",
                newName: "LocalItContractTemplateTypes");

            migrationBuilder.RenameTable(
                name: "LocalItContractRoles",
                schema: "dbo",
                newName: "LocalItContractRoles");

            migrationBuilder.RenameTable(
                name: "LocalInterfaceTypes",
                schema: "dbo",
                newName: "LocalInterfaceTypes");

            migrationBuilder.RenameTable(
                name: "LocalDataTypes",
                schema: "dbo",
                newName: "LocalDataTypes");

            migrationBuilder.RenameTable(
                name: "LocalDataProcessingRegistrationRoles",
                schema: "dbo",
                newName: "LocalDataProcessingRegistrationRoles");

            migrationBuilder.RenameTable(
                name: "LocalDataProcessingOversightOptions",
                schema: "dbo",
                newName: "LocalDataProcessingOversightOptions");

            migrationBuilder.RenameTable(
                name: "LocalDataProcessingDataResponsibleOptions",
                schema: "dbo",
                newName: "LocalDataProcessingDataResponsibleOptions");

            migrationBuilder.RenameTable(
                name: "LocalDataProcessingCountryOptions",
                schema: "dbo",
                newName: "LocalDataProcessingCountryOptions");

            migrationBuilder.RenameTable(
                name: "LocalDataProcessingBasisForTransferOptions",
                schema: "dbo",
                newName: "LocalDataProcessingBasisForTransferOptions");

            migrationBuilder.RenameTable(
                name: "LocalCriticalityTypes",
                schema: "dbo",
                newName: "LocalCriticalityTypes");

            migrationBuilder.RenameTable(
                name: "LocalBusinessTypes",
                schema: "dbo",
                newName: "LocalBusinessTypes");

            migrationBuilder.RenameTable(
                name: "LocalArchiveTypes",
                schema: "dbo",
                newName: "LocalArchiveTypes");

            migrationBuilder.RenameTable(
                name: "LocalArchiveTestLocations",
                schema: "dbo",
                newName: "LocalArchiveTestLocations");

            migrationBuilder.RenameTable(
                name: "LocalArchiveLocations",
                schema: "dbo",
                newName: "LocalArchiveLocations");

            migrationBuilder.RenameTable(
                name: "LocalAgreementElementTypes",
                schema: "dbo",
                newName: "LocalAgreementElementTypes");

            migrationBuilder.RenameTable(
                name: "LifeCycleTrackingEvents",
                schema: "dbo",
                newName: "LifeCycleTrackingEvents");

            migrationBuilder.RenameTable(
                name: "KLEUpdateHistoryItems",
                schema: "dbo",
                newName: "KLEUpdateHistoryItems");

            migrationBuilder.RenameTable(
                name: "KendoOrganizationalConfigurations",
                schema: "dbo",
                newName: "KendoOrganizationalConfigurations");

            migrationBuilder.RenameTable(
                name: "KendoColumnConfigurations",
                schema: "dbo",
                newName: "KendoColumnConfigurations");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageTechnicalSystemTypes",
                schema: "dbo",
                newName: "ItSystemUsageTechnicalSystemTypes");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageSensitiveDataLevels",
                schema: "dbo",
                newName: "ItSystemUsageSensitiveDataLevels");

            migrationBuilder.RenameTable(
                name: "ItSystemUsagePersonalDatas",
                schema: "dbo",
                newName: "ItSystemUsagePersonalDatas");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewUsingSystemUsageReadModels",
                schema: "dbo",
                newName: "ItSystemUsageOverviewUsingSystemUsageReadModels");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewUsedBySystemUsageReadModels",
                schema: "dbo",
                newName: "ItSystemUsageOverviewUsedBySystemUsageReadModels");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewTechnicalSystemTypeReadModel",
                schema: "dbo",
                newName: "ItSystemUsageOverviewTechnicalSystemTypeReadModel");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewTaskRefReadModels",
                schema: "dbo",
                newName: "ItSystemUsageOverviewTaskRefReadModels");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewSensitiveDataLevelReadModels",
                schema: "dbo",
                newName: "ItSystemUsageOverviewSensitiveDataLevelReadModels");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewRoleAssignmentReadModels",
                schema: "dbo",
                newName: "ItSystemUsageOverviewRoleAssignmentReadModels");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewRelevantOrgUnitReadModels",
                schema: "dbo",
                newName: "ItSystemUsageOverviewRelevantOrgUnitReadModels");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewReadModels",
                schema: "dbo",
                newName: "ItSystemUsageOverviewReadModels");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewLocalTaskRefReadModels",
                schema: "dbo",
                newName: "ItSystemUsageOverviewLocalTaskRefReadModels");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewItContractReadModels",
                schema: "dbo",
                newName: "ItSystemUsageOverviewItContractReadModels");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewInterfaceReadModels",
                schema: "dbo",
                newName: "ItSystemUsageOverviewInterfaceReadModels");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewDataProcessingRegistrationReadModels",
                schema: "dbo",
                newName: "ItSystemUsageOverviewDataProcessingRegistrationReadModels");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOverviewArchivePeriodReadModels",
                schema: "dbo",
                newName: "ItSystemUsageOverviewArchivePeriodReadModels");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageOrgUnitUsages",
                schema: "dbo",
                newName: "ItSystemUsageOrgUnitUsages");

            migrationBuilder.RenameTable(
                name: "ItSystemUsageArchive",
                schema: "dbo",
                newName: "ItSystemUsageArchive");

            migrationBuilder.RenameTable(
                name: "ItSystemUsage",
                schema: "dbo",
                newName: "ItSystemUsage");

            migrationBuilder.RenameTable(
                name: "ItSystemRoles",
                schema: "dbo",
                newName: "ItSystemRoles");

            migrationBuilder.RenameTable(
                name: "ItSystemRights",
                schema: "dbo",
                newName: "ItSystemRights");

            migrationBuilder.RenameTable(
                name: "ItSystemCategories",
                schema: "dbo",
                newName: "ItSystemCategories");

            migrationBuilder.RenameTable(
                name: "ItSystem",
                schema: "dbo",
                newName: "ItSystem");

            migrationBuilder.RenameTable(
                name: "ItInterface",
                schema: "dbo",
                newName: "ItInterface");

            migrationBuilder.RenameTable(
                name: "ItContractTypes",
                schema: "dbo",
                newName: "ItContractTypes");

            migrationBuilder.RenameTable(
                name: "ItContractTemplateTypes",
                schema: "dbo",
                newName: "ItContractTemplateTypes");

            migrationBuilder.RenameTable(
                name: "ItContractSupplierOverviewReadModels",
                schema: "dbo",
                newName: "ItContractSupplierOverviewReadModels");

            migrationBuilder.RenameTable(
                name: "ItContractSupplierOverviewAtCriticalityContractReadModels",
                schema: "dbo",
                newName: "ItContractSupplierOverviewAtCriticalityContractReadModels");

            migrationBuilder.RenameTable(
                name: "ItContractRoles",
                schema: "dbo",
                newName: "ItContractRoles");

            migrationBuilder.RenameTable(
                name: "ItContractRights",
                schema: "dbo",
                newName: "ItContractRights");

            migrationBuilder.RenameTable(
                name: "ItContractOverviewRoleAssignmentReadModels",
                schema: "dbo",
                newName: "ItContractOverviewRoleAssignmentReadModels");

            migrationBuilder.RenameTable(
                name: "ItContractOverviewReadModelSystemRelations",
                schema: "dbo",
                newName: "ItContractOverviewReadModelSystemRelations");

            migrationBuilder.RenameTable(
                name: "ItContractOverviewReadModels",
                schema: "dbo",
                newName: "ItContractOverviewReadModels");

            migrationBuilder.RenameTable(
                name: "ItContractOverviewReadModelItSystemUsages",
                schema: "dbo",
                newName: "ItContractOverviewReadModelItSystemUsages");

            migrationBuilder.RenameTable(
                name: "ItContractOverviewReadModelDataProcessingAgreements",
                schema: "dbo",
                newName: "ItContractOverviewReadModelDataProcessingAgreements");

            migrationBuilder.RenameTable(
                name: "ItContractItSystemUsages",
                schema: "dbo",
                newName: "ItContractItSystemUsages");

            migrationBuilder.RenameTable(
                name: "ItContractDataProcessingRegistrations",
                schema: "dbo",
                newName: "ItContractDataProcessingRegistrations");

            migrationBuilder.RenameTable(
                name: "ItContractAgreementElementTypes",
                schema: "dbo",
                newName: "ItContractAgreementElementTypes");

            migrationBuilder.RenameTable(
                name: "ItContract",
                schema: "dbo",
                newName: "ItContract");

            migrationBuilder.RenameTable(
                name: "InterfaceTypes",
                schema: "dbo",
                newName: "InterfaceTypes");

            migrationBuilder.RenameTable(
                name: "HelpTexts",
                schema: "dbo",
                newName: "HelpTexts");

            migrationBuilder.RenameTable(
                name: "ExternalReferences",
                schema: "dbo",
                newName: "ExternalReferences");

            migrationBuilder.RenameTable(
                name: "Exhibit",
                schema: "dbo",
                newName: "Exhibit");

            migrationBuilder.RenameTable(
                name: "EconomyStream",
                schema: "dbo",
                newName: "EconomyStream");

            migrationBuilder.RenameTable(
                name: "DataTypes",
                schema: "dbo",
                newName: "DataTypes");

            migrationBuilder.RenameTable(
                name: "DataRow",
                schema: "dbo",
                newName: "DataRow");

            migrationBuilder.RenameTable(
                name: "DataResponsibles",
                schema: "dbo",
                newName: "DataResponsibles");

            migrationBuilder.RenameTable(
                name: "DataProtectionAdvisors",
                schema: "dbo",
                newName: "DataProtectionAdvisors");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrations",
                schema: "dbo",
                newName: "DataProcessingRegistrations");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationRoles",
                schema: "dbo",
                newName: "DataProcessingRegistrationRoles");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationRoleAssignmentReadModels",
                schema: "dbo",
                newName: "DataProcessingRegistrationRoleAssignmentReadModels");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationRights",
                schema: "dbo",
                newName: "DataProcessingRegistrationRights");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationReadModels",
                schema: "dbo",
                newName: "DataProcessingRegistrationReadModels");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationOversightDates",
                schema: "dbo",
                newName: "DataProcessingRegistrationOversightDates");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationOrganizations",
                schema: "dbo",
                newName: "DataProcessingRegistrationOrganizations");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationItSystemUsages",
                schema: "dbo",
                newName: "DataProcessingRegistrationItSystemUsages");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationDataProcessingOversightOptions",
                schema: "dbo",
                newName: "DataProcessingRegistrationDataProcessingOversightOptions");

            migrationBuilder.RenameTable(
                name: "DataProcessingRegistrationDataProcessingCountryOptions",
                schema: "dbo",
                newName: "DataProcessingRegistrationDataProcessingCountryOptions");

            migrationBuilder.RenameTable(
                name: "DataProcessingOversightOptions",
                schema: "dbo",
                newName: "DataProcessingOversightOptions");

            migrationBuilder.RenameTable(
                name: "DataProcessingDataResponsibleOptions",
                schema: "dbo",
                newName: "DataProcessingDataResponsibleOptions");

            migrationBuilder.RenameTable(
                name: "DataProcessingCountryOptions",
                schema: "dbo",
                newName: "DataProcessingCountryOptions");

            migrationBuilder.RenameTable(
                name: "DataProcessingBasisForTransferOptions",
                schema: "dbo",
                newName: "DataProcessingBasisForTransferOptions");

            migrationBuilder.RenameTable(
                name: "CustomizedUiNodes",
                schema: "dbo",
                newName: "CustomizedUiNodes");

            migrationBuilder.RenameTable(
                name: "CriticalityTypes",
                schema: "dbo",
                newName: "CriticalityTypes");

            migrationBuilder.RenameTable(
                name: "CountryCodes",
                schema: "dbo",
                newName: "CountryCodes");

            migrationBuilder.RenameTable(
                name: "ContactPersons",
                schema: "dbo",
                newName: "ContactPersons");

            migrationBuilder.RenameTable(
                name: "Config",
                schema: "dbo",
                newName: "Config");

            migrationBuilder.RenameTable(
                name: "BusinessTypes",
                schema: "dbo",
                newName: "BusinessTypes");

            migrationBuilder.RenameTable(
                name: "BrokenLinkInInterfaces",
                schema: "dbo",
                newName: "BrokenLinkInInterfaces");

            migrationBuilder.RenameTable(
                name: "BrokenLinkInExternalReferences",
                schema: "dbo",
                newName: "BrokenLinkInExternalReferences");

            migrationBuilder.RenameTable(
                name: "BrokenExternalReferencesReports",
                schema: "dbo",
                newName: "BrokenExternalReferencesReports");

            migrationBuilder.RenameTable(
                name: "AttachedOptions",
                schema: "dbo",
                newName: "AttachedOptions");

            migrationBuilder.RenameTable(
                name: "ArchiveTypes",
                schema: "dbo",
                newName: "ArchiveTypes");

            migrationBuilder.RenameTable(
                name: "ArchiveTestLocations",
                schema: "dbo",
                newName: "ArchiveTestLocations");

            migrationBuilder.RenameTable(
                name: "ArchiveReference",
                schema: "dbo",
                newName: "ArchiveReference");

            migrationBuilder.RenameTable(
                name: "ArchivePeriod",
                schema: "dbo",
                newName: "ArchivePeriod");

            migrationBuilder.RenameTable(
                name: "ArchiveLocations",
                schema: "dbo",
                newName: "ArchiveLocations");

            migrationBuilder.RenameTable(
                name: "AgreementElementTypes",
                schema: "dbo",
                newName: "AgreementElementTypes");

            migrationBuilder.RenameTable(
                name: "AdviceUserRelations",
                schema: "dbo",
                newName: "AdviceUserRelations");

            migrationBuilder.RenameTable(
                name: "AdviceSents",
                schema: "dbo",
                newName: "AdviceSents");

            migrationBuilder.RenameTable(
                name: "Advice",
                schema: "dbo",
                newName: "Advice");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "TerminationDeadlineTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "TechnicalSystemTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SystemUsageCriticalityLevelTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SensitivePersonalDataTypes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SensitiveDataTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "RelationFrequencyTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "RegisterTypes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PurchaseFormTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ProcurementStrategyTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PriceRegulationTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PaymentModelTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PaymentFreqencyTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "OrganizationUnitRoles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "OrganizationUnit",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Organization",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "OptionExtendTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ItSystemRoles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ItSystemCategories",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ItSystem",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ItInterface",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ItContractTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ItContractTemplateTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ItContractRoles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ItContract",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "InterfaceTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DataTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DataProcessingRegistrations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DataProcessingRegistrationRoles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DataProcessingOversightOptions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DataProcessingDataResponsibleOptions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DataProcessingCountryOptions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DataProcessingBasisForTransferOptions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CriticalityTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CountryCodes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "BusinessTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ArchiveTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ArchiveTestLocations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ArchiveLocations",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AgreementElementTypes",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 150);

            migrationBuilder.CreateIndex(
                name: "UX_TaskKey",
                table: "TaskRef",
                column: "TaskKey",
                unique: true,
                filter: "[TaskKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_ContactPerson_Id",
                table: "Organization",
                column: "ContactPerson_Id",
                unique: true,
                filter: "[ContactPerson_Id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItSystemUsageOrgUnitUsages_ResponsibleItSystemUsage_Id",
                table: "ItSystemUsageOrgUnitUsages",
                column: "ResponsibleItSystemUsage_Id",
                unique: true,
                filter: "[ResponsibleItSystemUsage_Id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItContractItSystemUsages_ItSystemUsage_Id",
                table: "ItContractItSystemUsages",
                column: "ItSystemUsage_Id",
                unique: true,
                filter: "[ItSystemUsage_Id] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskRefItSystemUsageOptOut_ItSystemUsage_ItSystemUsage_Id",
                table: "TaskRefItSystemUsageOptOut",
                column: "ItSystemUsage_Id",
                principalTable: "ItSystemUsage",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskRefItSystemUsageOptOut_TaskRef_TaskRef_Id",
                table: "TaskRefItSystemUsageOptOut",
                column: "TaskRef_Id",
                principalTable: "TaskRef",
                principalColumn: "Id");
        }
    }
}
