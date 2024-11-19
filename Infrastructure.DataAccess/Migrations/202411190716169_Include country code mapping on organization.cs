namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Includecountrycodemappingonorganization : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.Organization", name: "CountryCode_Id", newName: "ForeignCountryCodeId");
            RenameIndex(table: "dbo.Organization", name: "IX_CountryCode_Id", newName: "IX_ForeignCountryCodeId");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.Organization", name: "IX_ForeignCountryCodeId", newName: "IX_CountryCode_Id");
            RenameColumn(table: "dbo.Organization", name: "ForeignCountryCodeId", newName: "CountryCode_Id");
        }
    }
}
