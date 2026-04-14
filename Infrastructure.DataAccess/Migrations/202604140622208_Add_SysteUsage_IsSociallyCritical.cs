namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Add_SysteUsage_IsSociallyCritical : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ItSystemUsage", "IsSociallyCritical", c => c.Int());
            AddColumn("dbo.ItSystemUsageOverviewReadModels", "IsSociallyCritical", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ItSystemUsageOverviewReadModels", "IsSociallyCritical");
            DropColumn("dbo.ItSystemUsage", "IsSociallyCritical");
        }
    }
}
