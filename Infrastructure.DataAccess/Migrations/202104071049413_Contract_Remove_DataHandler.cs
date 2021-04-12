using System.Runtime.Remoting.Lifetime;

namespace Infrastructure.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class Contract_Remove_DataHandler : DbMigration
    {
        public override void Up()
        {
            var leaseTime = LifetimeServices.LeaseTime;
            try
            {
                LifetimeServices.LeaseTime = TimeSpan.FromHours(1);
                DropForeignKey("dbo.ItContract", "DataHandlerId", "dbo.ItContract");
                DropIndex("dbo.ItContract", new[] { "DataHandlerId" });
                DropColumn("dbo.ItContract", "DataHandlerId");
            }
            finally
            {
                LifetimeServices.LeaseTime = leaseTime;
            }
        }

        public override void Down()
        {
            AddColumn("dbo.ItContract", "DataHandlerId", c => c.Int());
            CreateIndex("dbo.ItContract", "DataHandlerId");
            AddForeignKey("dbo.ItContract", "DataHandlerId", "dbo.ItContract", "Id");
        }
    }
}
