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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace OurPlace.API
{
    public static class LocationLogic
    {
        // Try to get the general locality of a point from Google
        public static async Task<PlaceLocality> GetLocality(double lat, double lon)
        {
            PlaceLocality locality = null;

            using (HttpClient client = new HttpClient())
            {
                string reqUrl = string.Format("https://maps.googleapis.com/maps/api/geocode/json?latlng={0},{1}&sensor=true&result_type=locality&key={2}",
                    lat, lon, Common.ConfidentialData.mapsk);
                var response = await client.GetStringAsync(reqUrl);
                Common.Models.GMapsResultColl ret = JsonConvert.DeserializeObject<Common.Models.GMapsResultColl>(response);

                if(ret != null && ret.results.Count > 0)
                {
                    Common.Models.GooglePlaceResult place = ret.results[0];
                    locality = new PlaceLocality
                    {
                        GooglePlaceId = place.place_id,
                        Latitude = new decimal(place.geometry.location.lat),
                        Longitude = new decimal(place.geometry.location.lng),
                        Name = place.address_components[0].long_name,
                        CreatedAt = DateTime.UtcNow
                    };
                }
            }

            return locality;
        }


        // http://stackoverflow.com/questions/32203770/linq-find-closest-coordinates
        private static IQueryable<Place> GetClosest(ApplicationDbContext db, double lat, double lon, int limit = 10)
        {
            decimal latitude = new decimal(lat);
            decimal longitude = new decimal(lon);

            // Try to get closest group, so we only compute over a subsection
            PlaceLocality closestLocality = db.PlaceLocalities.OrderBy(pl =>
                    (latitude - pl.Latitude) * (latitude - pl.Latitude) + (longitude - pl.Longitude) * (longitude - pl.Longitude)).FirstOrDefault();

            if(closestLocality != null)
            {
                return db.Places.Where(pl => pl.Locality.Id == closestLocality.Id);
            }

            // Resort to looking at all items
            return db.Places.OrderBy(pl =>
            (latitude - pl.Latitude) * (latitude - pl.Latitude) + (longitude - pl.Longitude) * (longitude - pl.Longitude))
            .Take(limit);
        }

        public static double GetDistanceBetween(double lat1, double lon1, double lat2, double lon2, char unit)
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) +
                          Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) *
                          Math.Cos(deg2rad(theta));
            dist = Math.Acos(dist);
            dist = rad2deg(dist);
            dist = dist * 60 * 1.1515;
            if (unit == 'K')
            {
                dist = dist * 1.609344;
            }
            return (dist);
        }
        private static double deg2rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }
        private static double rad2deg(double rad)
        {
            return (rad / Math.PI * 180.0);
        }

        public static IEnumerable<Place> GetPlacesNear(ApplicationDbContext db, double lat, double lon, int rangeMeters = 2500, int limit = 10)
        {
            // Get a list of the closest places
            IQueryable<Place> closestPlaces = GetClosest(db, lat, lon, limit);

            return closestPlaces.AsEnumerable().Where(pl =>
                GetDistanceBetween((double)lat, (double)lon, (double)pl.Latitude, (double)pl.Longitude, 'K') <= rangeMeters / 1000.0);
        }
    }
}