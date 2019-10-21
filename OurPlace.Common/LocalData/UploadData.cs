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
using SQLite;
using System;

namespace OurPlace.Common.LocalData
{
    public enum UploadType { NewActivity, Result, UpdatedActivity, NewCollection, UpdatedCollection }

    public class AppDataUpload
    {
        [PrimaryKey]
        public int ItemId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public UploadType UploadType { get; set; }
        public string ImageUrl { get; set; }
        public string JsonData { get; set; }
        public string FilesJson { get; set; }
        public string UploadRoute { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class FileUpload
    {
        public string LocalFilePath { get; set; }
        public string RemoteFilePath { get; set; }
    }

    public class ContentCache
    {
        [PrimaryKey]
        public int ActivityId { get; set; }
        public string JsonData { get; set; }
        public DateTime AddedAt { get; set; }
    }

    public class ActivityProgress
    {
        [PrimaryKey]
        public int ActivityId { get; set; }
        public int ActivityVersion { get; set; }
        public string JsonData { get; set; }
        public string AppTaskJson { get; set; }
        public string EnteredUsername { get; set; }
    }
}