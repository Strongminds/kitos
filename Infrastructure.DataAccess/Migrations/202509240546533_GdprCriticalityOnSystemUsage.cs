namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class GdprCriticalityOnSystemUsage : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ItSystemUsage", "GdprCriticality", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ItSystemUsage", "GdprCriticality");
        }
    }
}
