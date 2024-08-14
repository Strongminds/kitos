﻿namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Added_Data_Responsible : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.DataProcessingRegistrations", name: "DataProcessingDataResponsibleOption_Id", newName: "DataResponsible_Id");
            RenameIndex(table: "dbo.DataProcessingRegistrations", name: "IX_DataProcessingDataResponsibleOption_Id", newName: "IX_DataResponsible_Id");
            AddColumn("dbo.DataProcessingRegistrations", "DataResponsibleRemark", c => c.String());
            AddColumn("dbo.DataProcessingRegistrationReadModels", "DataResponsibleName", c => c.String(maxLength: 100));
            CreateIndex("dbo.DataProcessingRegistrationReadModels", "DataResponsibleName", name: "IX_DPR_DataResponsible");
        }
        
        public override void Down()
        {
            DropIndex("dbo.DataProcessingRegistrationReadModels", "IX_DPR_DataResponsible");
            DropColumn("dbo.DataProcessingRegistrationReadModels", "DataResponsibleName");
            DropColumn("dbo.DataProcessingRegistrations", "DataResponsibleRemark");
            RenameIndex(table: "dbo.DataProcessingRegistrations", name: "IX_DataResponsible_Id", newName: "IX_DataProcessingDataResponsibleOption_Id");
            RenameColumn(table: "dbo.DataProcessingRegistrations", name: "DataResponsible_Id", newName: "DataProcessingDataResponsibleOption_Id");
        }
    }
}
