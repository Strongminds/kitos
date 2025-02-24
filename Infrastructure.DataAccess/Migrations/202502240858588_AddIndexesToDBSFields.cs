namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIndexesToDBSFields : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.ItSystem", "DBSName", name: "ItSystem_IX_DBSName");
            CreateIndex("dbo.ItSystem", "DBSDataProcessorName", name: "ItSystem_IX_DBSDataProcessorName");
        }
        
        public override void Down()
        {
            DropIndex("dbo.ItSystem", "ItSystem_IX_DBSDataProcessorName");
            DropIndex("dbo.ItSystem", "ItSystem_IX_DBSName");
        }
    }
}
