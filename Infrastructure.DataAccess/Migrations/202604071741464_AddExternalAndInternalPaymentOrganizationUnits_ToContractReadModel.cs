namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddExternalAndInternalPaymentOrganizationUnits_ToContractReadModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ItContractOverviewReadModels", "ExternalPaymentOrganizationUnitsCsv", c => c.String());
            AddColumn("dbo.ItContractOverviewReadModels", "InternalPaymentOrganizationUnitsCsv", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ItContractOverviewReadModels", "InternalPaymentOrganizationUnitsCsv");
            DropColumn("dbo.ItContractOverviewReadModels", "ExternalPaymentOrganizationUnitsCsv");
        }
    }
}
