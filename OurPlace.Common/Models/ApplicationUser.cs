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
using System;

namespace OurPlace.Common.Models
{
    public class ApplicationUser
    {
        public string Id { get; set; }
        public string ImageUrl { get; set; }
        public DateTime DateCreated { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public bool Trusted { get; set; }

        public string AccessToken { get; set; }
        public DateTime AccessExpiresAt { get; set; }

        public string RefreshToken { get; set; }
        public DateTime RefreshExpiresAt { get; set; }

        public string CachedActivitiesJson { get; set; }
        public string RemoteCreatedActivitiesJson { get; set; }
        public string LocalCreatedActivitiesJson { get; set; }

    }

    public class LimitedApplicationUser
    {
        public string Id { get; set; }
        public string ImageUrl { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public bool Trusted { get; set; }
    }
}
