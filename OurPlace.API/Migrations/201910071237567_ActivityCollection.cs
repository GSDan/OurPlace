namespace OurPlace.API.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ActivityCollection : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ActivityCollections",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        CreatedAt = c.DateTime(nullable: false),
                        Description = c.String(),
                        ImageUrl = c.String(),
                        IsTrail = c.Boolean(nullable: false),
                        QRCodeUrl = c.String(),
                        Approved = c.Boolean(nullable: false),
                        InviteCode = c.String(),
                        SoftDeleted = c.Boolean(nullable: false),
                        CollectionVersionNumber = c.Int(nullable: false),
                        Application_Id = c.Int(),
                        Author_Id = c.String(maxLength: 128),
                        Location_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Applications", t => t.Application_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.Author_Id)
                .ForeignKey("dbo.Places", t => t.Location_Id)
                .Index(t => t.Application_Id)
                .Index(t => t.Author_Id)
                .Index(t => t.Location_Id);
            
            AddColumn("dbo.LearningActivities", "ActivityCollection_Id", c => c.Int());
            CreateIndex("dbo.LearningActivities", "ActivityCollection_Id");
            AddForeignKey("dbo.LearningActivities", "ActivityCollection_Id", "dbo.ActivityCollections", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ActivityCollections", "Location_Id", "dbo.Places");
            DropForeignKey("dbo.ActivityCollections", "Author_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.ActivityCollections", "Application_Id", "dbo.Applications");
            DropForeignKey("dbo.LearningActivities", "ActivityCollection_Id", "dbo.ActivityCollections");
            DropIndex("dbo.LearningActivities", new[] { "ActivityCollection_Id" });
            DropIndex("dbo.ActivityCollections", new[] { "Location_Id" });
            DropIndex("dbo.ActivityCollections", new[] { "Author_Id" });
            DropIndex("dbo.ActivityCollections", new[] { "Application_Id" });
            DropColumn("dbo.LearningActivities", "ActivityCollection_Id");
            DropTable("dbo.ActivityCollections");
        }
    }
}
