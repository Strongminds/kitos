namespace Infrastructure.DataAccess.Migrations
{
    using Infrastructure.DataAccess.Tools;
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateParentItSystemUsageUuidValues : DbMigration
    {
        public override void Up()
        {
            SqlResource(SqlMigrationScriptRepository.GetResourceName("Patch_ParentItSystemUsageUuid.sql"));
        }
        
        public override void Down()
        {
        }
    }
}
