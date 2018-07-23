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
    
    public partial class logTypeId : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.UsageLogs", name: "UsageLogType_Id", newName: "UsageLogTypeId");
            RenameIndex(table: "dbo.UsageLogs", name: "IX_UsageLogType_Id", newName: "IX_UsageLogTypeId");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.UsageLogs", name: "IX_UsageLogTypeId", newName: "IX_UsageLogType_Id");
            RenameColumn(table: "dbo.UsageLogs", name: "UsageLogTypeId", newName: "UsageLogType_Id");
        }
    }
}
