namespace OurPlace.API.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ActivityCollection_Update : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ActivityCollections", "IsPublic", c => c.Boolean(nullable: false));
            AddColumn("dbo.ActivityCollections", "ActivityOrder", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ActivityCollections", "ActivityOrder");
            DropColumn("dbo.ActivityCollections", "IsPublic");
        }
    }
}
