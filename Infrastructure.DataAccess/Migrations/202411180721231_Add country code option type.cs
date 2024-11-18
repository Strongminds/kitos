namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Addcountrycodeoptiontype : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CountryCodes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 150),
                        IsLocallyAvailable = c.Boolean(nullable: false),
                        IsObligatory = c.Boolean(nullable: false),
                        Description = c.String(),
                        IsEnabled = c.Boolean(nullable: false),
                        Priority = c.Int(nullable: false),
                        Uuid = c.Guid(nullable: false),
                        ObjectOwnerId = c.Int(nullable: false),
                        LastChanged = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        LastChangedByUserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.LastChangedByUserId)
                .ForeignKey("dbo.User", t => t.ObjectOwnerId)
                .Index(t => t.Uuid, unique: true, name: "UX_Option_Uuid")
                .Index(t => t.ObjectOwnerId)
                .Index(t => t.LastChangedByUserId);
            
            AddColumn("dbo.Organization", "CountryCode_Id", c => c.Int());
            CreateIndex("dbo.Organization", "CountryCode_Id");
            AddForeignKey("dbo.Organization", "CountryCode_Id", "dbo.CountryCodes", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Organization", "CountryCode_Id", "dbo.CountryCodes");
            DropForeignKey("dbo.CountryCodes", "ObjectOwnerId", "dbo.User");
            DropForeignKey("dbo.CountryCodes", "LastChangedByUserId", "dbo.User");
            DropIndex("dbo.CountryCodes", new[] { "LastChangedByUserId" });
            DropIndex("dbo.CountryCodes", new[] { "ObjectOwnerId" });
            DropIndex("dbo.CountryCodes", "UX_Option_Uuid");
            DropIndex("dbo.Organization", new[] { "CountryCode_Id" });
            DropColumn("dbo.Organization", "CountryCode_Id");
            DropTable("dbo.CountryCodes");
        }
    }
}
