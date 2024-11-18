namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Revertlocalcountrycodes : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.LocalCountryCodes", "LastChangedByUserId", "dbo.User");
            DropForeignKey("dbo.LocalCountryCodes", "ObjectOwnerId", "dbo.User");
            DropForeignKey("dbo.LocalCountryCodes", "OrganizationId", "dbo.Organization");
            DropIndex("dbo.LocalCountryCodes", new[] { "OrganizationId" });
            DropIndex("dbo.LocalCountryCodes", new[] { "ObjectOwnerId" });
            DropIndex("dbo.LocalCountryCodes", new[] { "LastChangedByUserId" });
            DropTable("dbo.LocalCountryCodes");
        }
        
        public override void Down()
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
                .PrimaryKey(t => t.Id);
            
            CreateIndex("dbo.LocalCountryCodes", "LastChangedByUserId");
            CreateIndex("dbo.LocalCountryCodes", "ObjectOwnerId");
            CreateIndex("dbo.LocalCountryCodes", "OrganizationId");
            AddForeignKey("dbo.LocalCountryCodes", "OrganizationId", "dbo.Organization", "Id", cascadeDelete: true);
            AddForeignKey("dbo.LocalCountryCodes", "ObjectOwnerId", "dbo.User", "Id");
            AddForeignKey("dbo.LocalCountryCodes", "LastChangedByUserId", "dbo.User", "Id");
        }
    }
}
