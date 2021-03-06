﻿#region copyright
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
using System;
using System.Collections.Generic;

namespace OurPlace.API.Models
{
    public class LearningActivity : Model
    {
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPublic { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string QRCodeUrl { get; set; }
        public bool Approved { get; set; }
        public string InviteCode { get; set; }
        public bool RequireUsername { get; set; }

        public bool SoftDeleted { get; set; }

        public virtual ApplicationUser Author { get; set; }
        public virtual ICollection<Place> Places { get; set; }
        public virtual ICollection<LearningTask> LearningTasks { get; set; }
        public virtual Application Application { get; set; }

        public int AppVersionNumber { get; set; }
        public int ActivityVersionNumber { get; set; }
    }
}
