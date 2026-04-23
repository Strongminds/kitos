CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260413095837_InitialBaseline', '10.0.6');

ALTER TABLE "ItContractOverviewReadModels" ADD "ExternalPaymentOrganizationUnitsCsv" nvarchar(max);

ALTER TABLE "ItContractOverviewReadModels" ADD "InternalPaymentOrganizationUnitsCsv" nvarchar(max);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260415045340_AddExternalAndInternalPaymentOrganizationUnits_ToContractReadModel', '10.0.6');


DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'ItSystem' AND column_name = 'SensitivePersonalDataTypeId'
    ) THEN
        ALTER TABLE "ItSystem" ADD COLUMN "SensitivePersonalDataTypeId" integer NULL;
        ALTER TABLE "ItSystem" ADD CONSTRAINT "FK_ItSystem_SensitivePersonalDataTypes_SensitivePersonalDataTypeId"
            FOREIGN KEY ("SensitivePersonalDataTypeId") REFERENCES "SensitivePersonalDataTypes" ("Id");
        CREATE INDEX "IX_ItSystem_SensitivePersonalDataTypeId" ON "ItSystem" ("SensitivePersonalDataTypeId");
    END IF;
END $$;


DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'ItSystemUsage' AND column_name = 'RegisterTypeId'
    ) THEN
        ALTER TABLE "ItSystemUsage" ADD COLUMN "RegisterTypeId" integer NULL;
        ALTER TABLE "ItSystemUsage" ADD CONSTRAINT "FK_ItSystemUsage_RegisterTypes_RegisterTypeId"
            FOREIGN KEY ("RegisterTypeId") REFERENCES "RegisterTypes" ("Id");
        CREATE INDEX "IX_ItSystemUsage_RegisterTypeId" ON "ItSystemUsage" ("RegisterTypeId");
    END IF;
END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260420093000_BridgeMissingColumnsFromEF6', '10.0.6');

