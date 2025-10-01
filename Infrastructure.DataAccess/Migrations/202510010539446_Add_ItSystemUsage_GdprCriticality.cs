namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Add_ItSystemUsage_GdprCriticality : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ItSystemUsage", "GdprCriticality", c => c.Int());
            AddColumn("dbo.ItSystemUsageOverviewReadModels", "GdprCriticality", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ItSystemUsageOverviewReadModels", "GdprCriticality");
            DropColumn("dbo.ItSystemUsage", "GdprCriticality");
        }
    }
}
