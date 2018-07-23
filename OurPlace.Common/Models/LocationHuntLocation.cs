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
    public class LocationHuntLocation : Map_Location
    {
        public bool? MapAvailable { get; set; } = true;

        public LocationHuntLocation(double _lat, double _lon, float _zoom, bool? allowMap) : base(_lat, _lon, _zoom)
        {
            MapAvailable = allowMap;
        }
    }
}
