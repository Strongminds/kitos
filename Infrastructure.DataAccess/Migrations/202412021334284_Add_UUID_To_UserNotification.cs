using System.Data.Entity.Migrations;
namespace Infrastructure.DataAccess.Migrations
{
    public partial class Add_UUID_To_UserNotification : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserNotifications", "Uuid", c => c.Guid(nullable: false));
            Sql(@"
                UPDATE dbo.UserNotifications 
                SET Uuid = NEWID() 
                WHERE Uuid IS NULL 
                   OR Uuid = '00000000-0000-0000-0000-000000000000'
            ");
            CreateIndex("dbo.UserNotifications", "Uuid", unique: true);
        }

        public override void Down()
        {
            DropIndex("dbo.UserNotifications", new[] { "Uuid" });
            DropColumn("dbo.UserNotifications", "Uuid");
        }
    }
}