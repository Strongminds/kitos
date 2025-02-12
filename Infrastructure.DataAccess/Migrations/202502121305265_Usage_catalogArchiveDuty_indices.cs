namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Usage_catalogArchiveDuty_indices : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.ItSystemUsageOverviewReadModels", "CatalogArchiveDuty", name: "ItSystemUsageOverviewReadModel_Index_CatalogArchiveDuty");
            CreateIndex("dbo.ItSystemUsageOverviewReadModels", "CatalogArchiveDutyComment", name: "ItSystemUsageOverviewReadModel_Index_CatalogArchiveDutyComment");
        }
        
        public override void Down()
        {
            DropIndex("dbo.ItSystemUsageOverviewReadModels", "ItSystemUsageOverviewReadModel_Index_CatalogArchiveDutyComment");
            DropIndex("dbo.ItSystemUsageOverviewReadModels", "ItSystemUsageOverviewReadModel_Index_CatalogArchiveDuty");
        }
    }
}
