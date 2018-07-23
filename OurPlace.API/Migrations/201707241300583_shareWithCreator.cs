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
    
    public partial class shareWithCreator : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ControlMeasures", "Creator_Id", "dbo.AspNetUsers");
            DropIndex("dbo.ControlMeasures", new[] { "Creator_Id" });
            AddColumn("dbo.CompletedActivities", "ShareWithCreator", c => c.Boolean(nullable: false));
            DropTable("dbo.ControlMeasures");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.ControlMeasures",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Description = c.String(),
                        Creator_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id);
            
            DropColumn("dbo.CompletedActivities", "ShareWithCreator");
            CreateIndex("dbo.ControlMeasures", "Creator_Id");
            AddForeignKey("dbo.ControlMeasures", "Creator_Id", "dbo.AspNetUsers", "Id");
        }
    }
}
