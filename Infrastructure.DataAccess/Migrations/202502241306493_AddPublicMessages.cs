namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPublicMessages : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PublicMessage",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Uuid = c.Guid(nullable: false),
                        Title = c.String(maxLength: 50),
                        LongDescription = c.String(),
                        Status = c.Int(),
                        ShortDescription = c.String(maxLength: 200),
                        Link = c.String(),
                        ObjectOwnerId = c.Int(nullable: false),
                        LastChanged = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        LastChangedByUserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.LastChangedByUserId)
                .ForeignKey("dbo.User", t => t.ObjectOwnerId)
                .Index(t => t.Uuid, unique: true, name: "UX_PublicMessage_Uuid")
                .Index(t => t.ObjectOwnerId)
                .Index(t => t.LastChangedByUserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PublicMessage", "ObjectOwnerId", "dbo.User");
            DropForeignKey("dbo.PublicMessage", "LastChangedByUserId", "dbo.User");
            DropIndex("dbo.PublicMessage", new[] { "LastChangedByUserId" });
            DropIndex("dbo.PublicMessage", new[] { "ObjectOwnerId" });
            DropIndex("dbo.PublicMessage", "UX_PublicMessage_Uuid");
            DropTable("dbo.PublicMessage");
        }
    }
}
