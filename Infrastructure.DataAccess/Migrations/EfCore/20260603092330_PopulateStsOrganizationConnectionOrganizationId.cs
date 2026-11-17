using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class PopulateStsOrganizationConnectionOrganizationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The legacy EF6 schema uses a shared primary key pattern where Id is NOT an
            // identity column and serves as the FK to Organization. In that case OrganizationId
            // may be unpopulated (0 or mismatched). Databases created from the new Baseline.sql
            // have Id as an IDENTITY with OrganizationId as the real FK — those must not be touched.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.identity_columns ic
                    INNER JOIN sys.tables t ON ic.object_id = t.object_id
                    WHERE t.name = 'StsOrganizationConnections' AND ic.name = 'Id'
                )
                BEGIN
                    UPDATE dbo.StsOrganizationConnections SET OrganizationId = Id WHERE OrganizationId != Id OR OrganizationId = 0;
                END
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
