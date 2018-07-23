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
    
    public partial class learningTaskActivityRelationship : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.LearningActivityPlaces", newName: "PlaceLearningActivities");
            DropIndex("dbo.LearningTasks", new[] { "LearningActivity_Id" });
            RenameColumn(table: "dbo.LearningTasks", name: "LearningActivity_Id", newName: "LearningActivityId");
            DropPrimaryKey("dbo.PlaceLearningActivities");
            AlterColumn("dbo.LearningTasks", "LearningActivityId", c => c.Int(nullable: false));
            AddPrimaryKey("dbo.PlaceLearningActivities", new[] { "Place_Id", "LearningActivity_Id" });
            CreateIndex("dbo.LearningTasks", "LearningActivityId");
        }
        
        public override void Down()
        {
            DropIndex("dbo.LearningTasks", new[] { "LearningActivityId" });
            DropPrimaryKey("dbo.PlaceLearningActivities");
            AlterColumn("dbo.LearningTasks", "LearningActivityId", c => c.Int());
            AddPrimaryKey("dbo.PlaceLearningActivities", new[] { "LearningActivity_Id", "Place_Id" });
            RenameColumn(table: "dbo.LearningTasks", name: "LearningActivityId", newName: "LearningActivity_Id");
            CreateIndex("dbo.LearningTasks", "LearningActivity_Id");
            RenameTable(name: "dbo.PlaceLearningActivities", newName: "LearningActivityPlaces");
        }
    }
}
