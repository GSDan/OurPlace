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
using Microsoft.AspNet.Identity.Owin;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using OurPlace.API.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace OurPlace.API.Controllers
{
    public class ParkLearnAPIController : ApiController
    {
        protected const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        protected static Random rand = new Random();

        private static UsageLogType useLT;
        private static UsageLogType errLT;

        protected ApplicationUserManager UserManager
        {
            get
            {
                return Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
        }
        protected ApplicationDbContext db
        {
            get
            {
                return Request.GetOwinContext().Get<ApplicationDbContext>();
            }
        }

        protected async Task<ApplicationUser> GetUser()
        {
            ApplicationUser thisUser = null;
            if (User?.Identity?.Name != null)
            {
                thisUser = await UserManager.FindByNameAsync(User.Identity.Name);
            }
            return thisUser;
        }

        protected async Task MakeLog(Dictionary<string, string> data = null)
        {
            try
            {
                if (useLT == null)
                {
                    useLT = db.LogTypes.Where(t => t.Name == "USE").FirstOrDefault();
                    if (useLT == null) return;
                }

                db.UsageLogs.Add(new UsageLog
                {
                    User = await GetUser(),
                    Route = Request.RequestUri.AbsolutePath,
                    Data = JsonConvert.SerializeObject(data),
                    UsageLogTypeId = useLT.Id
                });
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                await LogError(e.Message);
            }
        }

        protected async Task LogError(string data)
        {
            if (errLT == null)
            {
                errLT = db.LogTypes.Where(t => t.Name == "ERROR").FirstOrDefault();
                if (errLT == null) return;
            }

            db.UsageLogs.Add(new UsageLog
            {
                User = await GetUser(),
                Route = Request.RequestUri.AbsolutePath,
                Data = data,
                UsageLogTypeId = errLT.Id
            });
            await db.SaveChangesAsync();
        }

        protected async Task<List<Place>> ProcessPlacesInNewContent(ICollection<Place> places, ApplicationUser currentUser, List<Place> finalPlaces = null)
        {
            // Go through the creation's Places, adding them to the database if necessary
            if (finalPlaces == null)
            {
                finalPlaces = new List<Place>();
            }
            else
            {
                // avoiding creating new, which seems to cause conflicts
                finalPlaces.Clear();
            }

            if (places == null) return finalPlaces;

            for (int i = 0; i < places.Count; i++)
            {
                Place thisPlace = places.ElementAt(i);
                Place existing = await db.Places.Where(p => p.GooglePlaceId == thisPlace.GooglePlaceId).FirstOrDefaultAsync();
                if (existing != null)
                {
                    finalPlaces.Add(existing);
                }
                else
                {
                    Common.Models.GMapsResult result;

                    using (HttpClient client = new HttpClient())
                    {
                        string reqUrl = string.Format("https://maps.googleapis.com/maps/api/place/details/json?placeid={0}&key={1}",
                            thisPlace.GooglePlaceId, Common.ConfidentialData.mapsk);
                        var response = await client.GetStringAsync(reqUrl);
                        result = JsonConvert.DeserializeObject<Common.Models.GMapsResult>(response);
                    }
                    if (result.status == "OK")
                    {
                        double lat = result.result.geometry.location.lat;
                        double lon = result.result.geometry.location.lng;

                        Place finalPlace = new Place
                        {
                            GooglePlaceId = result.result.place_id,
                            Latitude = new decimal(lat),
                            Longitude = new decimal(lon),
                            Location = ServerUtils.CreatePoint(lat, lon),
                            Name = result.result.name,
                            CreatedAt = DateTime.UtcNow,
                            AddedBy = currentUser
                        };

                        // Check for parent locality
                        PlaceLocality locality = await LocationLogic.GetLocality(lat, lon);
                        if (locality != null)
                        {
                            PlaceLocality existingLocality = await db.PlaceLocalities
                                .Where(p => p.GooglePlaceId == locality.GooglePlaceId).FirstOrDefaultAsync();
                            if (existingLocality == null)
                            {
                                finalPlace.Locality = db.PlaceLocalities.Add(locality);
                                await db.SaveChangesAsync();
                            }
                            else
                            {
                                finalPlace.Locality = existingLocality;
                            }
                        }

                        finalPlaces.Add(db.Places.Add(finalPlace));
                    }

                }
            }

            return finalPlaces;
        }

        protected async Task<string> GenerateQR(string codeName, string codeData)
        {
            string qrCodeUrl = "qrCodes/" + codeName + ".png";
            Bitmap qrCode = ServerUtils.GenerateQRCode(codeData, true);
            CloudBlobContainer appContainer = ServerUtils.GetCloudBlobContainer();
            CloudBlockBlob blob = appContainer.GetBlockBlobReference(qrCodeUrl);
            using (MemoryStream stream = new MemoryStream())
            {
                qrCode.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                blob.Properties.ContentType = "image/png";
                stream.Seek(0, SeekOrigin.Begin);
                await blob.UploadFromStreamAsync(stream);
            }

            return qrCodeUrl;
        }
    }
}