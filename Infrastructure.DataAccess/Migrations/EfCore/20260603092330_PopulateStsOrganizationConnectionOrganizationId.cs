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
            // The StsOrganizationConnections table uses a shared primary key pattern where
            // Id is the FK to Organization. Populate OrganizationId from Id so EF Core can
            // use OrganizationId as the tracked FK column.
            migrationBuilder.Sql("UPDATE dbo.StsOrganizationConnections SET OrganizationId = Id WHERE OrganizationId != Id OR OrganizationId = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
