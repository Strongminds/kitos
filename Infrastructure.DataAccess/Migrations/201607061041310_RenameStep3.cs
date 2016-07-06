namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class RenameStep3 : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.OrganizationRoles", newName: "OrganizationUnitRoles");
        }

        public override void Down()
        {
            RenameTable(name: "dbo.OrganizationUnitRoles", newName: "OrganizationRoles");
        }
    }
}
