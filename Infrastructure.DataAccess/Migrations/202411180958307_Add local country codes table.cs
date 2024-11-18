namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Addlocalcountrycodestable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LocalCountryCodes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Description = c.String(),
                        OrganizationId = c.Int(nullable: false),
                        OptionId = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        ObjectOwnerId = c.Int(),
                        LastChanged = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        LastChangedByUserId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.LastChangedByUserId)
                .ForeignKey("dbo.User", t => t.ObjectOwnerId)
                .ForeignKey("dbo.Organization", t => t.OrganizationId, cascadeDelete: true)
                .Index(t => t.OrganizationId)
                .Index(t => t.ObjectOwnerId)
                .Index(t => t.LastChangedByUserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LocalCountryCodes", "OrganizationId", "dbo.Organization");
            DropForeignKey("dbo.LocalCountryCodes", "ObjectOwnerId", "dbo.User");
            DropForeignKey("dbo.LocalCountryCodes", "LastChangedByUserId", "dbo.User");
            DropIndex("dbo.LocalCountryCodes", new[] { "LastChangedByUserId" });
            DropIndex("dbo.LocalCountryCodes", new[] { "ObjectOwnerId" });
            DropIndex("dbo.LocalCountryCodes", new[] { "OrganizationId" });
            DropTable("dbo.LocalCountryCodes");
        }
    }
}
