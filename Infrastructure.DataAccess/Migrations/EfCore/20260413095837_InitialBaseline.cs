using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class InitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // For SQL Server: Schema is applied via DeploymentScripts/Baseline.sql before this migration runs.
            // This migration is a placeholder - the Up() body does nothing, and the migration history
            // entry is pre-inserted via sqlcmd in DbMigrations.ps1.
            //
            // For PostgreSQL: Use the pre-generated Baseline.PostgreSql.sql script which contains
            // all table creation DDL. This migration is marked as applied without executing Up().
            //
            // Existing EF6-migrated databases also have this migration pre-marked without executing Up().
            
            // This Up() method intentionally does not create any tables.
            // The schema creation is handled externally for all scenarios.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}