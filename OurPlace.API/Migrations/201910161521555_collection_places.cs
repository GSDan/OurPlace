namespace OurPlace.API.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class collection_places : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ActivityCollections", "Location_Id", "dbo.Places");
            DropIndex("dbo.ActivityCollections", new[] { "Location_Id" });
            CreateTable(
                "dbo.PlaceActivityCollections",
                c => new
                    {
                        Place_Id = c.Int(nullable: false),
                        ActivityCollection_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Place_Id, t.ActivityCollection_Id })
                .ForeignKey("dbo.Places", t => t.Place_Id, cascadeDelete: true)
                .ForeignKey("dbo.ActivityCollections", t => t.ActivityCollection_Id, cascadeDelete: true)
                .Index(t => t.Place_Id)
                .Index(t => t.ActivityCollection_Id);
            
            DropColumn("dbo.ActivityCollections", "Location_Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ActivityCollections", "Location_Id", c => c.Int());
            DropForeignKey("dbo.PlaceActivityCollections", "ActivityCollection_Id", "dbo.ActivityCollections");
            DropForeignKey("dbo.PlaceActivityCollections", "Place_Id", "dbo.Places");
            DropIndex("dbo.PlaceActivityCollections", new[] { "ActivityCollection_Id" });
            DropIndex("dbo.PlaceActivityCollections", new[] { "Place_Id" });
            DropTable("dbo.PlaceActivityCollections");
            CreateIndex("dbo.ActivityCollections", "Location_Id");
            AddForeignKey("dbo.ActivityCollections", "Location_Id", "dbo.Places", "Id");
        }
    }
}
