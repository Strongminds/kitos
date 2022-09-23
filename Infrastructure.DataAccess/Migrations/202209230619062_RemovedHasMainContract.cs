﻿namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovedHasMainContract : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.ItSystemUsageOverviewReadModels", "ItSystemUsageOverviewReadModel_Index_HasMainContract");

            //Migrate MainContractIsActvie from null to false
            Sql(@"UPDATE dbo.ItSystemUsageOverviewReadModels 
                  SET MainContractIsActive = CASE
									            WHEN MainContractIsActive IS NULL THEN 0
									            ELSE MainContractIsActive
								               END;"
            );

            AlterColumn("dbo.ItSystemUsageOverviewReadModels", "MainContractIsActive", c => c.Boolean(nullable: false));
            DropColumn("dbo.ItSystemUsageOverviewReadModels", "HasMainContract");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ItSystemUsageOverviewReadModels", "HasMainContract", c => c.Boolean(nullable: false));
            AlterColumn("dbo.ItSystemUsageOverviewReadModels", "MainContractIsActive", c => c.Boolean());
            CreateIndex("dbo.ItSystemUsageOverviewReadModels", "HasMainContract", name: "ItSystemUsageOverviewReadModel_Index_HasMainContract");
        }
    }
}
