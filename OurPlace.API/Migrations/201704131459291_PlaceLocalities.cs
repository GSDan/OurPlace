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
    
    public partial class PlaceLocalities : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PlaceLocalities",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Latitude = c.Decimal(nullable: false, precision: 11, scale: 7),
                        Longitude = c.Decimal(nullable: false, precision: 11, scale: 7),
                        GooglePlaceId = c.String(),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.Places", "Locality_Id", c => c.Int());
            CreateIndex("dbo.Places", "Locality_Id");
            AddForeignKey("dbo.Places", "Locality_Id", "dbo.PlaceLocalities", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Places", "Locality_Id", "dbo.PlaceLocalities");
            DropIndex("dbo.Places", new[] { "Locality_Id" });
            DropColumn("dbo.Places", "Locality_Id");
            DropTable("dbo.PlaceLocalities");
        }
    }
}
