namespace OurPlace.API.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    using System.Data.Entity.Spatial;
    
    public partial class Geography : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Places", "Location", c => c.Geography());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Places", "Location");
        }
    }
}
