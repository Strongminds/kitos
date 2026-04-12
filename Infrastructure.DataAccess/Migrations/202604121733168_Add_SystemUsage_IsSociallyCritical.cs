namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Add_SystemUsage_IsSociallyCritical : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ItSystemUsage", "IsSociallyCritical", c => c.Boolean(nullable: false));
            AddColumn("dbo.ItSystemUsageOverviewReadModels", "IsSociallyCritical", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ItSystemUsageOverviewReadModels", "IsSociallyCritical");
            DropColumn("dbo.ItSystemUsage", "IsSociallyCritical");
        }
    }
}
