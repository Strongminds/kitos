﻿namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Add_Tracking_Events : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LifeCycleTrackingEvents",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        EventType = c.Int(nullable: false),
                        OccurredAtUtc = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        EntityUuid = c.Guid(nullable: false),
                        EntityType = c.Int(nullable: false),
                        OptionalOrganizationReferenceId = c.Int(),
                        OptionalAccessModifier = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Organization", t => t.OptionalOrganizationReferenceId)
                .Index(t => new { t.OptionalOrganizationReferenceId, t.OccurredAtUtc, t.EntityType, t.EventType }, name: "IX_Org_OccurredAt_EntityType_EventType")
                .Index(t => new { t.OptionalOrganizationReferenceId, t.OptionalAccessModifier, t.OccurredAtUtc, t.EntityType, t.EventType }, name: "IX_Org_AccessModifier_OccurredAt_EntityType_EventType")
                .Index(t => t.EntityUuid);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LifeCycleTrackingEvents", "OptionalOrganizationReferenceId", "dbo.Organization");
            DropIndex("dbo.LifeCycleTrackingEvents", new[] { "EntityUuid" });
            DropIndex("dbo.LifeCycleTrackingEvents", "IX_Org_AccessModifier_OccurredAt_EntityType_EventType");
            DropIndex("dbo.LifeCycleTrackingEvents", "IX_Org_OccurredAt_EntityType_EventType");
            DropTable("dbo.LifeCycleTrackingEvents");
        }
    }
}
