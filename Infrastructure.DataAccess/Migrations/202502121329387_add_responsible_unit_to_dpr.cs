namespace Infrastructure.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class add_responsible_unit_to_dpr : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DataProcessingRegistrations", "ResponsibleOrganizationUnitId", c => c.Int());
            AddColumn("dbo.DataProcessingRegistrationReadModels", "ResponsibleOrgUnitId", c => c.Int());
            AddColumn("dbo.DataProcessingRegistrationReadModels", "ResponsibleOrgUnitName", c => c.String());
            CreateIndex("dbo.DataProcessingRegistrations", "ResponsibleOrganizationUnitId");
            CreateIndex("dbo.DataProcessingRegistrationReadModels", "ResponsibleOrgUnitId", name: "IX_DPR_ResponsibleOrgUnitId");
            AddForeignKey("dbo.DataProcessingRegistrations", "ResponsibleOrganizationUnitId", "dbo.OrganizationUnit", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.DataProcessingRegistrations", "ResponsibleOrganizationUnitId", "dbo.OrganizationUnit");
            DropIndex("dbo.DataProcessingRegistrationReadModels", "IX_DPR_ResponsibleOrgUnitId");
            DropIndex("dbo.DataProcessingRegistrations", new[] { "ResponsibleOrganizationUnitId" });
            DropColumn("dbo.DataProcessingRegistrationReadModels", "ResponsibleOrgUnitName");
            DropColumn("dbo.DataProcessingRegistrationReadModels", "ResponsibleOrgUnitId");
            DropColumn("dbo.DataProcessingRegistrations", "ResponsibleOrganizationUnitId");
        }
    }
}
