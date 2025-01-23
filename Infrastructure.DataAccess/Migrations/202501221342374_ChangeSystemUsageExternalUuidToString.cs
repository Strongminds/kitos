namespace Infrastructure.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ChangeSystemUsageExternalUuidToString : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.ItSystemUsageOverviewReadModels", "ExternalSystemUuid", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.ItSystemUsageOverviewReadModels", "ExternalSystemUuid", c => c.Guid());
        }
    }
}
