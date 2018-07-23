#region copyright
/*
    OurPlace is a mobile learning platform, designed to support communities
    in creating and sharing interactive learning activities about the places they care most about.
    https://github.com/GSDan/OurPlace
    Copyright (C) 2018 Dan Richardson

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see https://www.gnu.org/licenses.
*/
#endregion
namespace OurPlace.API.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CompletedActivities : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.CompletedTasks", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.CompletedTasks", "UserDevice_Id", "dbo.UserDevices");
            DropIndex("dbo.CompletedTasks", new[] { "User_Id" });
            DropIndex("dbo.CompletedTasks", new[] { "UserDevice_Id" });
            CreateTable(
                "dbo.CompletedActivities",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false),
                        LearningActivity_Id = c.Int(),
                        User_Id = c.String(maxLength: 128),
                        UserDevice_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.LearningActivities", t => t.LearningActivity_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .ForeignKey("dbo.UserDevices", t => t.UserDevice_Id)
                .Index(t => t.LearningActivity_Id)
                .Index(t => t.User_Id)
                .Index(t => t.UserDevice_Id);
            
            AddColumn("dbo.CompletedTasks", "ParentSubmission_Id", c => c.Int());
            CreateIndex("dbo.CompletedTasks", "ParentSubmission_Id");
            AddForeignKey("dbo.CompletedTasks", "ParentSubmission_Id", "dbo.CompletedActivities", "Id");
            DropColumn("dbo.CompletedTasks", "FinishedAt");
            DropColumn("dbo.CompletedTasks", "User_Id");
            DropColumn("dbo.CompletedTasks", "UserDevice_Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.CompletedTasks", "UserDevice_Id", c => c.Int());
            AddColumn("dbo.CompletedTasks", "User_Id", c => c.String(maxLength: 128));
            AddColumn("dbo.CompletedTasks", "FinishedAt", c => c.DateTime());
            DropForeignKey("dbo.CompletedTasks", "ParentSubmission_Id", "dbo.CompletedActivities");
            DropForeignKey("dbo.CompletedActivities", "UserDevice_Id", "dbo.UserDevices");
            DropForeignKey("dbo.CompletedActivities", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.CompletedActivities", "LearningActivity_Id", "dbo.LearningActivities");
            DropIndex("dbo.CompletedTasks", new[] { "ParentSubmission_Id" });
            DropIndex("dbo.CompletedActivities", new[] { "UserDevice_Id" });
            DropIndex("dbo.CompletedActivities", new[] { "User_Id" });
            DropIndex("dbo.CompletedActivities", new[] { "LearningActivity_Id" });
            DropColumn("dbo.CompletedTasks", "ParentSubmission_Id");
            DropTable("dbo.CompletedActivities");
            CreateIndex("dbo.CompletedTasks", "UserDevice_Id");
            CreateIndex("dbo.CompletedTasks", "User_Id");
            AddForeignKey("dbo.CompletedTasks", "UserDevice_Id", "dbo.UserDevices", "Id");
            AddForeignKey("dbo.CompletedTasks", "User_Id", "dbo.AspNetUsers", "Id");
        }
    }
}
