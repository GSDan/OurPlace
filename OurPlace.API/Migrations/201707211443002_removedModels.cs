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
    
    public partial class removedModels : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Hazards", "HazardType_Id", "dbo.HazardTypes");
            DropForeignKey("dbo.Hazards", "LearningTask_Id", "dbo.LearningTasks");
            DropForeignKey("dbo.Feedbacks", "Place_Id", "dbo.Places");
            DropForeignKey("dbo.Feedbacks", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Hazards", "Place_Id", "dbo.Places");
            DropForeignKey("dbo.Events", "CoordinatingUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.EventAttendees", "Event_Id", "dbo.Events");
            DropForeignKey("dbo.EventAttendees", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.EventAttendees", "UserRole_Id", "dbo.UserRoles");
            DropIndex("dbo.Hazards", new[] { "HazardType_Id" });
            DropIndex("dbo.Hazards", new[] { "LearningTask_Id" });
            DropIndex("dbo.Hazards", new[] { "Place_Id" });
            DropIndex("dbo.Feedbacks", new[] { "Place_Id" });
            DropIndex("dbo.Feedbacks", new[] { "User_Id" });
            DropIndex("dbo.EventAttendees", new[] { "Event_Id" });
            DropIndex("dbo.EventAttendees", new[] { "User_Id" });
            DropIndex("dbo.EventAttendees", new[] { "UserRole_Id" });
            DropIndex("dbo.Events", new[] { "CoordinatingUser_Id" });
            DropTable("dbo.Hazards");
            DropTable("dbo.HazardTypes");
            DropTable("dbo.Feedbacks");
            DropTable("dbo.EventAttendees");
            DropTable("dbo.Events");
            DropTable("dbo.UserRoles");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.UserRoles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Role = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Events",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(),
                        AccessKey = c.String(),
                        CoordinatingUser_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.EventAttendees",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Event_Id = c.Int(),
                        User_Id = c.String(maxLength: 128),
                        UserRole_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Feedbacks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false),
                        Message = c.String(),
                        Place_Id = c.Int(),
                        User_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.HazardTypes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Hazards",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Description = c.String(),
                        ResidualRiskRating = c.Int(nullable: false),
                        HazardType_Id = c.Int(),
                        LearningTask_Id = c.Int(),
                        Place_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateIndex("dbo.Events", "CoordinatingUser_Id");
            CreateIndex("dbo.EventAttendees", "UserRole_Id");
            CreateIndex("dbo.EventAttendees", "User_Id");
            CreateIndex("dbo.EventAttendees", "Event_Id");
            CreateIndex("dbo.Feedbacks", "User_Id");
            CreateIndex("dbo.Feedbacks", "Place_Id");
            CreateIndex("dbo.Hazards", "Place_Id");
            CreateIndex("dbo.Hazards", "LearningTask_Id");
            CreateIndex("dbo.Hazards", "HazardType_Id");
            AddForeignKey("dbo.EventAttendees", "UserRole_Id", "dbo.UserRoles", "Id");
            AddForeignKey("dbo.EventAttendees", "User_Id", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.EventAttendees", "Event_Id", "dbo.Events", "Id");
            AddForeignKey("dbo.Events", "CoordinatingUser_Id", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.Hazards", "Place_Id", "dbo.Places", "Id");
            AddForeignKey("dbo.Feedbacks", "User_Id", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.Feedbacks", "Place_Id", "dbo.Places", "Id");
            AddForeignKey("dbo.Hazards", "LearningTask_Id", "dbo.LearningTasks", "Id");
            AddForeignKey("dbo.Hazards", "HazardType_Id", "dbo.HazardTypes", "Id");
        }
    }
}
