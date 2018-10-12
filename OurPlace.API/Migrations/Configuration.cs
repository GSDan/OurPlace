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
    using Common;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<OurPlace.API.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(OurPlace.API.Models.ApplicationDbContext context)
        {
            //  This method will be called after migrating to the latest version.
            context.Applications.AddOrUpdate(
                app => app.Name,
                new Models.Application
                {
                    Name = "Park:Learn",
                    Description = "The mobile outdoor learning tool",
                    LogoUrl = "https://openlabdata.blob.core.windows.net/parklearn/icons/OurPlaceLogo.png"
                });

            context.LogTypes.AddOrUpdate(logt => logt.Name,
                new Models.UsageLogType
                {
                    Name = "USE"
                },
                new Models.UsageLogType
                {
                    Name = "ERROR"
                });

            context.TaskTypes.AddOrUpdate(
                    tt => tt.IdName,
                    new Models.TaskType
                    {
                        Order = 0,
                        ReqResponse = false,
                        IdName = "INFO",
                        ReqFileUpload = false,
                        DisplayName = "Information",
                        Description = "Some additional information about the activity's topic, with an optional accompanying image.",
                        IconUrl = ConfidentialData.storage + "icons/info.png"
                    },
                    new Models.TaskType
                    {
                        Order = 1,
                        ReqResponse = false,
                        IdName = "LISTEN_AUDIO",
                        ReqFileUpload = false,
                        DisplayName = "Listen to Audio",
                        Description = "Listen to a given audio recording.",
                        IconUrl = ConfidentialData.storage + "icons/listenAudio.png"
                    },
                    new Models.TaskType
                    {
                        Order = 2,
                        ReqResponse = true,
                        IdName = "TAKE_PHOTO",
                        ReqFileUpload = true,
                        DisplayName = "Take a Photo",
                        Description = "Take a photograph using the device's camera.",
                        IconUrl = ConfidentialData.storage + "icons/takePhoto.png"
                    },
                    new Models.TaskType
                    {
                        Order = 3,
                        ReqResponse = true,
                        IdName = "MATCH_PHOTO",
                        ReqFileUpload = true,
                        DisplayName = "Photo Match",
                        Description = "Use the camera to match an existing photo.",
                        IconUrl = ConfidentialData.storage + "icons/matchPhoto.png"
                    },
                    new Models.TaskType
                    {
                        Order = 4,
                        ReqResponse = true,
                        IdName = "DRAW",
                        ReqFileUpload = true,
                        DisplayName = "Draw a Picture",
                        Description = "Use the paint tool to draw a picture!",
                        IconUrl = ConfidentialData.storage + "icons/draw.png"
                    },
                    new Models.TaskType
                    {
                        Order = 5,
                        ReqResponse = true,
                        IdName = "DRAW_PHOTO",
                        ReqFileUpload = true,
                        DisplayName = "Draw on Photo",
                        Description = "Use the paint tool to draw on top of a taken photo!",
                        IconUrl = ConfidentialData.storage + "icons/drawPhoto.png"
                    },
                    new Models.TaskType
                    {
                        Order = 6,
                        ReqResponse = true,
                        IdName = "TAKE_VIDEO",
                        ReqFileUpload = true,
                        DisplayName = "Record Video",
                        Description = "Record a video using the camera.",
                        IconUrl = ConfidentialData.storage + "icons/recordVideo.png"
                    },
                     new Models.TaskType
                     {
                         Order = 7,
                         ReqResponse = true,
                         IdName = "REC_AUDIO",
                         ReqFileUpload = true,
                         DisplayName = "Record Audio",
                         Description = "Record an audio clip using the device's microphone.",
                         IconUrl = ConfidentialData.storage + "icons/recordAudio.png"
                     },
                     new Models.TaskType
                     {
                         Order = 8,
                         ReqResponse = true,
                         IdName = "MAP_MARK",
                         DisplayName = "Map Marking",
                         ReqFileUpload = false,
                         Description = "Mark your location onto a map.",
                         IconUrl = ConfidentialData.storage + "icons/location.png"
                     },
                     new Models.TaskType
                     {
                         Order = 9,
                         ReqResponse = false,
                         IdName = "LOC_HUNT",
                         ReqFileUpload = false,
                         DisplayName = "Location Hunt",
                         Description = "Hunt down a given coordinate using a tracking tool!",
                         IconUrl = ConfidentialData.storage + "icons/locationHunt.png"
                     },
                     new Models.TaskType
                     {
                         Order = 10,
                         ReqResponse = false,
                         IdName = "QR_SCAN",
                         ReqFileUpload = false,
                         DisplayName = "Scan the QR Code",
                         Description = "Find and scan the correct QR code",
                         IconUrl = ConfidentialData.storage + "icons/locationHunt.png"
                     },
                     new Models.TaskType
                     {
                         Order = 11,
                         ReqResponse = true,
                         IdName = "MULT_CHOICE",
                         ReqFileUpload = false,
                         DisplayName = "Multiple Choice",
                         Description = "Choose an answer from a given set of options.",
                         IconUrl = ConfidentialData.storage + "icons/multipleChoice.png"
                     },
                      new Models.TaskType
                      {
                          Order = 12,
                          ReqResponse = true,
                          IdName = "ENTER_TEXT",
                          ReqFileUpload = false,
                          DisplayName = "Text Entry",
                          Description = "Enter a written response into a text field.",
                          IconUrl = ConfidentialData.storage + "icons/textEntry.png"
                      }
                );

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
        }
    }
}
