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
            // Idempotent guards are used because some EF6 databases may already contain these objects.

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ItSystem' AND COLUMN_NAME = 'SensitivePersonalDataTypeId')
                    ALTER TABLE [ItSystem] ADD [SensitivePersonalDataTypeId] int NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ItSystem_SensitivePersonalDataTypes_SensitivePersonalDataTypeId')
                    ALTER TABLE [ItSystem] ADD CONSTRAINT [FK_ItSystem_SensitivePersonalDataTypes_SensitivePersonalDataTypeId]
                        FOREIGN KEY ([SensitivePersonalDataTypeId]) REFERENCES [SensitivePersonalDataTypes] ([Id]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('ItSystem') AND name = 'IX_ItSystem_SensitivePersonalDataTypeId')
                    CREATE INDEX [IX_ItSystem_SensitivePersonalDataTypeId] ON [ItSystem] ([SensitivePersonalDataTypeId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ItSystemUsage' AND COLUMN_NAME = 'RegisterTypeId')
                    ALTER TABLE [ItSystemUsage] ADD [RegisterTypeId] int NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ItSystemUsage_RegisterTypes_RegisterTypeId')
                    ALTER TABLE [ItSystemUsage] ADD CONSTRAINT [FK_ItSystemUsage_RegisterTypes_RegisterTypeId]
                        FOREIGN KEY ([RegisterTypeId]) REFERENCES [RegisterTypes] ([Id]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('ItSystemUsage') AND name = 'IX_ItSystemUsage_RegisterTypeId')
                    CREATE INDEX [IX_ItSystemUsage_RegisterTypeId] ON [ItSystemUsage] ([RegisterTypeId]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItSystemUsage_RegisterTypeId",
                table: "ItSystemUsage");

            migrationBuilder.DropForeignKey(
                name: "FK_ItSystemUsage_RegisterTypes_RegisterTypeId",
                table: "ItSystemUsage");

            migrationBuilder.DropColumn(
                name: "RegisterTypeId",
                table: "ItSystemUsage");

            migrationBuilder.DropIndex(
                name: "IX_ItSystem_SensitivePersonalDataTypeId",
                table: "ItSystem");

            migrationBuilder.DropForeignKey(
                name: "FK_ItSystem_SensitivePersonalDataTypes_SensitivePersonalDataTypeId",
                table: "ItSystem");

            migrationBuilder.DropColumn(
                name: "SensitivePersonalDataTypeId",
                table: "ItSystem");
        }
    }
}
