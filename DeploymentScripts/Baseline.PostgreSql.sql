-- OBSOLETE
-- Do not use this script to bootstrap a PostgreSQL database.
--
-- The canonical PostgreSQL bootstrap flow is:
-- 1. Apply DeploymentScripts/Baseline.PostgreSql.FullModel.sql
-- 2. Initialize dbo.__EFMigrationsHistory with the InitialBaseline migration
-- 3. Run the remaining EF Core migrations
--
-- Use DeploymentScripts/DbMigrations.ps1 or the migration tool, both of which implement
-- that sequence. This file deliberately fails fast to avoid silently creating a drifted schema.
DO $$
BEGIN
    RAISE EXCEPTION 'DeploymentScripts/Baseline.PostgreSql.sql is obsolete. Use Baseline.PostgreSql.FullModel.sql together with DbMigrations.ps1 or Tools.MigrateSQLServer2Postgres.';
END $$;
