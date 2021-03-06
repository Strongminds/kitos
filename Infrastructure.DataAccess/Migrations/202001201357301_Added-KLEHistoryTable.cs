namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedKLEHistoryTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.KLEUpdateHistoryItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Version = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        ObjectOwnerId = c.Int(),
                        LastChanged = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        LastChangedByUserId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.LastChangedByUserId)
                .ForeignKey("dbo.User", t => t.ObjectOwnerId)
                .Index(t => t.ObjectOwnerId)
                .Index(t => t.LastChangedByUserId);
            
            AlterColumn("dbo.TaskRef", "TaskKey", c => c.String(maxLength: 50));
            CreateIndex("dbo.TaskRef", "TaskKey", unique: true, name: "UX_TaskKey");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.KLEUpdateHistoryItems", "ObjectOwnerId", "dbo.User");
            DropForeignKey("dbo.KLEUpdateHistoryItems", "LastChangedByUserId", "dbo.User");
            DropIndex("dbo.KLEUpdateHistoryItems", new[] { "LastChangedByUserId" });
            DropIndex("dbo.KLEUpdateHistoryItems", new[] { "ObjectOwnerId" });
            DropIndex("dbo.TaskRef", "UX_TaskKey");
            AlterColumn("dbo.TaskRef", "TaskKey", c => c.String());
            DropTable("dbo.KLEUpdateHistoryItems");
        }
    }
}
