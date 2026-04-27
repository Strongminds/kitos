using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class BridgeMissingColumnsFromEF6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // These columns exist in the EF Core model but were never added by EF6 migrations on
            // existing databases. All other structural differences (shadow FK column names such as
            // ItSystemUsage_Id, TaskRef_Id, etc.) use EF6's naming convention, which EF Core inherits
            // as shadow property names — so existing EF6 databases already have the correct column names
            // and no renaming is required here.

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ItSystem') AND name = 'SensitivePersonalDataTypeId')
BEGIN
    ALTER TABLE [ItSystem] ADD [SensitivePersonalDataTypeId] int NULL;
    ALTER TABLE [ItSystem] ADD CONSTRAINT [FK_ItSystem_SensitivePersonalDataTypes_SensitivePersonalDataTypeId]
        FOREIGN KEY ([SensitivePersonalDataTypeId]) REFERENCES [SensitivePersonalDataTypes] ([Id]);
    CREATE INDEX [IX_ItSystem_SensitivePersonalDataTypeId] ON [ItSystem] ([SensitivePersonalDataTypeId]);
END");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ItSystemUsage') AND name = 'RegisterTypeId')
BEGIN
    ALTER TABLE [ItSystemUsage] ADD [RegisterTypeId] int NULL;
    ALTER TABLE [ItSystemUsage] ADD CONSTRAINT [FK_ItSystemUsage_RegisterTypes_RegisterTypeId]
        FOREIGN KEY ([RegisterTypeId]) REFERENCES [RegisterTypes] ([Id]);
    CREATE INDEX [IX_ItSystemUsage_RegisterTypeId] ON [ItSystemUsage] ([RegisterTypeId]);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ItSystemUsage') AND name = 'RegisterTypeId')
BEGIN
    DROP INDEX [IX_ItSystemUsage_RegisterTypeId] ON [ItSystemUsage];
    ALTER TABLE [ItSystemUsage] DROP CONSTRAINT [FK_ItSystemUsage_RegisterTypes_RegisterTypeId];
    ALTER TABLE [ItSystemUsage] DROP COLUMN [RegisterTypeId];
END");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ItSystem') AND name = 'SensitivePersonalDataTypeId')
BEGIN
    DROP INDEX [IX_ItSystem_SensitivePersonalDataTypeId] ON [ItSystem];
    ALTER TABLE [ItSystem] DROP CONSTRAINT [FK_ItSystem_SensitivePersonalDataTypes_SensitivePersonalDataTypeId];
    ALTER TABLE [ItSystem] DROP COLUMN [SensitivePersonalDataTypeId];
END");
        }
    }
}
