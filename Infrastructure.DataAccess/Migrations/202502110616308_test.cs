namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class test : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ItSystemUsage", "ContainsAITechnology", c => c.Boolean());
            AddColumn("dbo.ItSystemUsageOverviewReadModels", "ContainsAITechnology", c => c.Boolean());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ItSystemUsageOverviewReadModels", "ContainsAITechnology");
            DropColumn("dbo.ItSystemUsage", "ContainsAITechnology");
        }
    }
}
