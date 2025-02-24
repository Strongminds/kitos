﻿namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDBSFieldsToItSystem : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ItSystem", "DBSName", c => c.String(maxLength: 100));
            AddColumn("dbo.ItSystem", "DBSDataProcessorName", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ItSystem", "DBSDataProcessorName");
            DropColumn("dbo.ItSystem", "DBSName");
        }
    }
}
