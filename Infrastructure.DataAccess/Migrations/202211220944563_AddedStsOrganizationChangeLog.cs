﻿namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedStsOrganizationChangeLog : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.StsOrganizationChangeLogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StsOrganizationConnectionId = c.Int(nullable: false),
                        UserId = c.Int(),
                        Origin = c.Int(nullable: false),
                        LogTime = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        ObjectOwnerId = c.Int(nullable: false),
                        LastChanged = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        LastChangedByUserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.User", t => t.LastChangedByUserId)
                .ForeignKey("dbo.User", t => t.ObjectOwnerId)
                .ForeignKey("dbo.StsOrganizationConnections", t => t.StsOrganizationConnectionId, cascadeDelete: true)
                .ForeignKey("dbo.User", t => t.UserId)
                .Index(t => t.StsOrganizationConnectionId)
                .Index(t => t.UserId, name: "IX_ChangeLogName")
                .Index(t => t.Origin, name: "IX_ChangeLogOrigin")
                .Index(t => t.LogTime)
                .Index(t => t.ObjectOwnerId)
                .Index(t => t.LastChangedByUserId);
            
            CreateTable(
                "dbo.StsOrganizationConsequenceLogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ChangeLogId = c.Int(nullable: false),
                        Uuid = c.Guid(nullable: false),
                        Name = c.String(nullable: false),
                        Type = c.Int(nullable: false),
                        Description = c.String(nullable: false),
                        ObjectOwnerId = c.Int(nullable: false),
                        LastChanged = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        LastChangedByUserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.StsOrganizationChangeLogs", t => t.ChangeLogId, cascadeDelete: true)
                .ForeignKey("dbo.User", t => t.LastChangedByUserId)
                .ForeignKey("dbo.User", t => t.ObjectOwnerId)
                .Index(t => t.ChangeLogId)
                .Index(t => t.Uuid, name: "IX_StsOrganizationConsequenceUuid")
                .Index(t => t.Type, name: "IX_StsOrganizationConsequenceType")
                .Index(t => t.ObjectOwnerId)
                .Index(t => t.LastChangedByUserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.StsOrganizationChangeLogs", "UserId", "dbo.User");
            DropForeignKey("dbo.StsOrganizationChangeLogs", "StsOrganizationConnectionId", "dbo.StsOrganizationConnections");
            DropForeignKey("dbo.StsOrganizationChangeLogs", "ObjectOwnerId", "dbo.User");
            DropForeignKey("dbo.StsOrganizationChangeLogs", "LastChangedByUserId", "dbo.User");
            DropForeignKey("dbo.StsOrganizationConsequenceLogs", "ObjectOwnerId", "dbo.User");
            DropForeignKey("dbo.StsOrganizationConsequenceLogs", "LastChangedByUserId", "dbo.User");
            DropForeignKey("dbo.StsOrganizationConsequenceLogs", "ChangeLogId", "dbo.StsOrganizationChangeLogs");
            DropIndex("dbo.StsOrganizationConsequenceLogs", new[] { "LastChangedByUserId" });
            DropIndex("dbo.StsOrganizationConsequenceLogs", new[] { "ObjectOwnerId" });
            DropIndex("dbo.StsOrganizationConsequenceLogs", "IX_StsOrganizationConsequenceType");
            DropIndex("dbo.StsOrganizationConsequenceLogs", "IX_StsOrganizationConsequenceUuid");
            DropIndex("dbo.StsOrganizationConsequenceLogs", new[] { "ChangeLogId" });
            DropIndex("dbo.StsOrganizationChangeLogs", new[] { "LastChangedByUserId" });
            DropIndex("dbo.StsOrganizationChangeLogs", new[] { "ObjectOwnerId" });
            DropIndex("dbo.StsOrganizationChangeLogs", new[] { "LogTime" });
            DropIndex("dbo.StsOrganizationChangeLogs", "IX_ChangeLogOrigin");
            DropIndex("dbo.StsOrganizationChangeLogs", "IX_ChangeLogName");
            DropIndex("dbo.StsOrganizationChangeLogs", new[] { "StsOrganizationConnectionId" });
            DropTable("dbo.StsOrganizationConsequenceLogs");
            DropTable("dbo.StsOrganizationChangeLogs");
        }
    }
}
