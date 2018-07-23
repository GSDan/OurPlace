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
    
    public partial class initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Applications",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Description = c.String(),
                        LogoUrl = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.CompletedTasks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedAt = c.DateTime(nullable: false),
                        FinishedAt = c.DateTime(),
                        JsonData = c.String(),
                        EventTask_Id = c.Int(),
                        User_Id = c.String(maxLength: 128),
                        UserDevice_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.LearningTasks", t => t.EventTask_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .ForeignKey("dbo.UserDevices", t => t.UserDevice_Id)
                .Index(t => t.EventTask_Id)
                .Index(t => t.User_Id)
                .Index(t => t.UserDevice_Id);
            
            CreateTable(
                "dbo.LearningTasks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Description = c.String(),
                        Order = c.Int(nullable: false),
                        JsonData = c.String(),
                        Author_Id = c.String(maxLength: 128),
                        LearningTask_Id = c.Int(),
                        TaskType_Id = c.Int(),
                        LearningActivity_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.Author_Id)
                .ForeignKey("dbo.LearningTasks", t => t.LearningTask_Id)
                .ForeignKey("dbo.TaskTypes", t => t.TaskType_Id)
                .ForeignKey("dbo.LearningActivities", t => t.LearningActivity_Id)
                .Index(t => t.Author_Id)
                .Index(t => t.LearningTask_Id)
                .Index(t => t.TaskType_Id)
                .Index(t => t.LearningActivity_Id);
            
            CreateTable(
                "dbo.AspNetUsers",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        ImageUrl = c.String(),
                        DateCreated = c.DateTime(nullable: false),
                        FirstName = c.String(),
                        AuthProvider = c.String(),
                        Surname = c.String(),
                        Trusted = c.Boolean(nullable: false),
                        Email = c.String(maxLength: 256),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");
            
            CreateTable(
                "dbo.AspNetUserClaims",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserLogins",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserRoles",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        RoleId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
            
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
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.HazardTypes", t => t.HazardType_Id)
                .ForeignKey("dbo.LearningTasks", t => t.LearningTask_Id)
                .ForeignKey("dbo.Places", t => t.Place_Id)
                .Index(t => t.HazardType_Id)
                .Index(t => t.LearningTask_Id)
                .Index(t => t.Place_Id);
            
            CreateTable(
                "dbo.HazardTypes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.TaskTypes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DisplayName = c.String(),
                        IdName = c.String(),
                        IconUrl = c.String(),
                        Description = c.String(),
                        ReqFileUpload = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.UserDevices",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Device_Id = c.Int(),
                        User_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Devices", t => t.Device_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .Index(t => t.Device_Id)
                .Index(t => t.User_Id);
            
            CreateTable(
                "dbo.Devices",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DeviceName = c.String(),
                        Platform = c.String(),
                        DeviceId = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ControlMeasures",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Description = c.String(),
                        Creator_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.Creator_Id)
                .Index(t => t.Creator_Id);
            
            CreateTable(
                "dbo.EventAttendees",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Event_Id = c.Int(),
                        User_Id = c.String(maxLength: 128),
                        UserRole_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Events", t => t.Event_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .ForeignKey("dbo.UserRoles", t => t.UserRole_Id)
                .Index(t => t.Event_Id)
                .Index(t => t.User_Id)
                .Index(t => t.UserRole_Id);
            
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
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.CoordinatingUser_Id)
                .Index(t => t.CoordinatingUser_Id);
            
            CreateTable(
                "dbo.UserRoles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Role = c.String(),
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
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Places", t => t.Place_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .Index(t => t.Place_Id)
                .Index(t => t.User_Id);
            
            CreateTable(
                "dbo.Places",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Latitude = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Longitude = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ImageUrl = c.String(),
                        ContactEmail = c.String(),
                        GooglePlaceId = c.String(),
                        CreatedAt = c.DateTime(nullable: false),
                        AddedBy_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.AddedBy_Id)
                .Index(t => t.AddedBy_Id);
            
            CreateTable(
                "dbo.LearningActivities",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        CreatedAt = c.DateTime(nullable: false),
                        IsPublic = c.Boolean(nullable: false),
                        Description = c.String(),
                        ImageUrl = c.String(),
                        Approved = c.Boolean(nullable: false),
                        InviteCode = c.String(),
                        Application_Id = c.Int(),
                        Author_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Applications", t => t.Application_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.Author_Id)
                .Index(t => t.Application_Id)
                .Index(t => t.Author_Id);
            
            CreateTable(
                "dbo.AspNetRoles",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");
            
            CreateTable(
                "dbo.UsageLogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Data = c.String(),
                        UsageLogType_Id = c.Int(),
                        User_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.UsageLogTypes", t => t.UsageLogType_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .Index(t => t.UsageLogType_Id)
                .Index(t => t.User_Id);
            
            CreateTable(
                "dbo.UsageLogTypes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.LearningActivityPlaces",
                c => new
                    {
                        LearningActivity_Id = c.Int(nullable: false),
                        Place_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.LearningActivity_Id, t.Place_Id })
                .ForeignKey("dbo.LearningActivities", t => t.LearningActivity_Id, cascadeDelete: true)
                .ForeignKey("dbo.Places", t => t.Place_Id, cascadeDelete: true)
                .Index(t => t.LearningActivity_Id)
                .Index(t => t.Place_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UsageLogs", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.UsageLogs", "UsageLogType_Id", "dbo.UsageLogTypes");
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.Feedbacks", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.Hazards", "Place_Id", "dbo.Places");
            DropForeignKey("dbo.Feedbacks", "Place_Id", "dbo.Places");
            DropForeignKey("dbo.Places", "AddedBy_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.LearningActivityPlaces", "Place_Id", "dbo.Places");
            DropForeignKey("dbo.LearningActivityPlaces", "LearningActivity_Id", "dbo.LearningActivities");
            DropForeignKey("dbo.LearningTasks", "LearningActivity_Id", "dbo.LearningActivities");
            DropForeignKey("dbo.LearningActivities", "Author_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.LearningActivities", "Application_Id", "dbo.Applications");
            DropForeignKey("dbo.EventAttendees", "UserRole_Id", "dbo.UserRoles");
            DropForeignKey("dbo.EventAttendees", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.EventAttendees", "Event_Id", "dbo.Events");
            DropForeignKey("dbo.Events", "CoordinatingUser_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.ControlMeasures", "Creator_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.CompletedTasks", "UserDevice_Id", "dbo.UserDevices");
            DropForeignKey("dbo.UserDevices", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.UserDevices", "Device_Id", "dbo.Devices");
            DropForeignKey("dbo.CompletedTasks", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.CompletedTasks", "EventTask_Id", "dbo.LearningTasks");
            DropForeignKey("dbo.LearningTasks", "TaskType_Id", "dbo.TaskTypes");
            DropForeignKey("dbo.Hazards", "LearningTask_Id", "dbo.LearningTasks");
            DropForeignKey("dbo.Hazards", "HazardType_Id", "dbo.HazardTypes");
            DropForeignKey("dbo.LearningTasks", "LearningTask_Id", "dbo.LearningTasks");
            DropForeignKey("dbo.LearningTasks", "Author_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropIndex("dbo.LearningActivityPlaces", new[] { "Place_Id" });
            DropIndex("dbo.LearningActivityPlaces", new[] { "LearningActivity_Id" });
            DropIndex("dbo.UsageLogs", new[] { "User_Id" });
            DropIndex("dbo.UsageLogs", new[] { "UsageLogType_Id" });
            DropIndex("dbo.AspNetRoles", "RoleNameIndex");
            DropIndex("dbo.LearningActivities", new[] { "Author_Id" });
            DropIndex("dbo.LearningActivities", new[] { "Application_Id" });
            DropIndex("dbo.Places", new[] { "AddedBy_Id" });
            DropIndex("dbo.Feedbacks", new[] { "User_Id" });
            DropIndex("dbo.Feedbacks", new[] { "Place_Id" });
            DropIndex("dbo.Events", new[] { "CoordinatingUser_Id" });
            DropIndex("dbo.EventAttendees", new[] { "UserRole_Id" });
            DropIndex("dbo.EventAttendees", new[] { "User_Id" });
            DropIndex("dbo.EventAttendees", new[] { "Event_Id" });
            DropIndex("dbo.ControlMeasures", new[] { "Creator_Id" });
            DropIndex("dbo.UserDevices", new[] { "User_Id" });
            DropIndex("dbo.UserDevices", new[] { "Device_Id" });
            DropIndex("dbo.Hazards", new[] { "Place_Id" });
            DropIndex("dbo.Hazards", new[] { "LearningTask_Id" });
            DropIndex("dbo.Hazards", new[] { "HazardType_Id" });
            DropIndex("dbo.AspNetUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "UserId" });
            DropIndex("dbo.AspNetUserLogins", new[] { "UserId" });
            DropIndex("dbo.AspNetUserClaims", new[] { "UserId" });
            DropIndex("dbo.AspNetUsers", "UserNameIndex");
            DropIndex("dbo.LearningTasks", new[] { "LearningActivity_Id" });
            DropIndex("dbo.LearningTasks", new[] { "TaskType_Id" });
            DropIndex("dbo.LearningTasks", new[] { "LearningTask_Id" });
            DropIndex("dbo.LearningTasks", new[] { "Author_Id" });
            DropIndex("dbo.CompletedTasks", new[] { "UserDevice_Id" });
            DropIndex("dbo.CompletedTasks", new[] { "User_Id" });
            DropIndex("dbo.CompletedTasks", new[] { "EventTask_Id" });
            DropTable("dbo.LearningActivityPlaces");
            DropTable("dbo.UsageLogTypes");
            DropTable("dbo.UsageLogs");
            DropTable("dbo.AspNetRoles");
            DropTable("dbo.LearningActivities");
            DropTable("dbo.Places");
            DropTable("dbo.Feedbacks");
            DropTable("dbo.UserRoles");
            DropTable("dbo.Events");
            DropTable("dbo.EventAttendees");
            DropTable("dbo.ControlMeasures");
            DropTable("dbo.Devices");
            DropTable("dbo.UserDevices");
            DropTable("dbo.TaskTypes");
            DropTable("dbo.HazardTypes");
            DropTable("dbo.Hazards");
            DropTable("dbo.AspNetUserRoles");
            DropTable("dbo.AspNetUserLogins");
            DropTable("dbo.AspNetUserClaims");
            DropTable("dbo.AspNetUsers");
            DropTable("dbo.LearningTasks");
            DropTable("dbo.CompletedTasks");
            DropTable("dbo.Applications");
        }
    }
}
