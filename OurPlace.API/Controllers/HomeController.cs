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
using Newtonsoft.Json;
using OurPlace.API.Models;
using OurPlace.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace OurPlace.API.Controllers
{
    public class HomeController : Site.OurPlaceSiteController
    {
        public async Task<ActionResult> Index()
        {
            ViewBag.Title = "OurPlace";

            await MakeLog();

            ViewData["MapsJKey"] = ConfidentialData.mapsk;

            List<Place> places = db.Places.ToList();
            List<PlaceMapPin> mapPins = new List<PlaceMapPin>();

            foreach(Place place in places)
            {
                mapPins.Add(new PlaceMapPin
                {
                    GooglePlaceId = place.GooglePlaceId,
                    ImageUrl = place.ImageUrl,
                    Latitude = place.Latitude,
                    Longitude = place.Longitude,
                    Name = place.Name,
                    NumActivities = place.Activities.Count()
                });
            }

            ViewData["MapPins"] = mapPins;

            return View();
        }
    }
}
