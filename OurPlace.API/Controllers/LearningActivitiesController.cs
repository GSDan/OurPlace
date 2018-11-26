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
using Microsoft.AspNet.Identity;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using OurPlace.API.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using LearningTask = OurPlace.API.Models.LearningTask;

namespace OurPlace.API.Controllers
{
    public class LearningActivitiesController : ParkLearnAPIController
    {
        private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static Random rand = new Random();

        private IEnumerable<Common.Models.LimitedLearningActivity> GetAllWhere(Func<LearningActivity, bool> predicate)
        {
            return db.LearningActivities.Where(predicate)
               .Select(a => new Common.Models.LimitedLearningActivity()
               {
                   Id = a.Id,
                   CreatedAt = a.CreatedAt,
                   Name = a.Name,
                   Description = a.Description,
                   IsPublic = a.IsPublic,
                   ImageUrl = a.ImageUrl,
                   Approved = a.Approved,
                   InviteCode = a.InviteCode,
                   RequireUsername = a.RequireUsername,
                   AppVersionNumber = a.AppVersionNumber,
                   Application = new Common.Models.Application
                   {
                       Id = a.Application.Id,
                       Name = a.Application.Name,
                       Description = a.Application.Description,
                       LogoUrl = a.Application.LogoUrl
                   },
                   Author = new Common.Models.LimitedApplicationUser
                   {
                       Id = a.Author.Id,
                       FirstName = a.Author.FirstName,
                       Surname = a.Author.Surname,
                       ImageUrl = a.Author.ImageUrl
                   },
                   Places = a.Places.Select(p => new Common.Models.Place
                   {
                       Id = p.Id,
                       Name = p.Name,
                       GooglePlaceId = p.GooglePlaceId,
                       Latitude = p.Latitude,
                       Longitude = p.Longitude
                   }),
                   LearningTasks = a.LearningTasks.Where(t => !t.SoftDeleted).Select(t => new Common.Models.LearningTask
                   {
                       Id = t.Id,
                       Description = t.Description,
                       JsonData = t.JsonData,
                       Order = t.Order,
                       TaskType = new Common.Models.TaskType
                       {
                           Id = t.TaskType.Id,
                           Description = t.TaskType.Description,
                           IconUrl = t.TaskType.IconUrl,
                           DisplayName = t.TaskType.DisplayName,
                           ReqFileUpload = t.TaskType.ReqFileUpload,
                           IdName = t.TaskType.IdName
                       },
                       ChildTasks = t.ChildTasks.Where(ct => !ct.SoftDeleted).Select(ct => new Common.Models.LearningTask
                       {
                           Id = ct.Id,
                           Description = ct.Description,
                           JsonData = ct.JsonData,
                           Order = ct.Order,
                           TaskType = new Common.Models.TaskType
                           {
                               Id = ct.TaskType.Id,
                               Description = ct.TaskType.Description,
                               IconUrl = ct.TaskType.IconUrl,
                               DisplayName = ct.TaskType.DisplayName,
                               ReqFileUpload = ct.TaskType.ReqFileUpload,
                               IdName = ct.TaskType.IdName
                           }
                       })
                   })
               }).ToList();
        }

        // GET: api/LearningActivities
        public async Task<HttpResponseMessage> GetLearningActivities()
        {
            ApplicationUser thisUser = await GetUser();
            // Get activities which are public and approved, or made by the current user

            if (thisUser == null)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Please log in");

            var resp = Request.CreateResponse(HttpStatusCode.OK);
            resp.Content = new StringContent(
                JsonConvert.SerializeObject(
                    GetAllWhere(a => !a.SoftDeleted && ((a.IsPublic && (a.Approved || thisUser.Trusted)) || a.Author.Id == thisUser.Id)),
                    new JsonSerializerSettings() {
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 5
                    }), Encoding.UTF8, "application/json");

            await MakeLog();

            return resp;
        }

        // GET: api/LearningActivities
        public async Task<HttpResponseMessage> GetFeed(double lat = 0, double lon = 0)
        {
            ApplicationUser thisUser = await GetUser();

            if (thisUser == null)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Please log in");

            List<Common.Models.LimitedActivityFeedSection> feed = new List<Common.Models.LimitedActivityFeedSection>();

            if(lat != 0 || lon != 0)
            {
                IEnumerable<Place> places = LocationLogic.GetPlacesNear(db, lat, lon, 2500, 3);

                foreach(Place pl in places)
                {
                    // Get activities which are near the user's position, public and approved
                    List<Common.Models.LimitedLearningActivity> actsHere = GetAllWhere(a => !a.SoftDeleted && (a.Places.Any(l => l.Id == pl.Id) && (a.Approved || thisUser.Trusted) && a.IsPublic)).Take(12).ToList();

                    if(actsHere != null && actsHere.Count > 0)
                    {
                        feed.Add(new Common.Models.LimitedActivityFeedSection
                        {
                            Title = string.Format("Activities at {0}", pl.Name),
                            Description = string.Format("It looks like you're near {0}! Here are some activities that have been made there.", pl.Name),
                            Activities = actsHere
                        });
                    }
                }
            }

            feed.Add(new Common.Models.LimitedActivityFeedSection
            {
                Title = "Recently Uploaded",
                Description = "Here are some of the latest activities that have been uploaded",
                // Get the most recent activities which are public and approved, or made by the current user
                Activities = GetAllWhere(a => !a.SoftDeleted && ((a.IsPublic && (a.Approved || thisUser.Trusted)) || a.Author.Id == thisUser.Id))
                    .OrderByDescending(a => a.CreatedAt).Take(8).ToList()
            });

            var resp = Request.CreateResponse(HttpStatusCode.OK);
            resp.Content = new StringContent(
                JsonConvert.SerializeObject(
                    feed,
                    new JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        MaxDepth = 7
                    }), Encoding.UTF8, "application/json");

            await MakeLog(new Dictionary<string, string>(){ { "lat", lat.ToString() }, {"lon", lon.ToString() } } );

            return resp;
        }

        // GET: api/LearningActivities
        public async Task<HttpResponseMessage> GetFromUser(string creatorId)
        {
            string userId = User?.Identity?.GetUserId();
            // Get activities created by the given creator. 
            // If the creator is the current user, include private and unapproved ones

            if (string.IsNullOrWhiteSpace(userId))
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Please log in");

            var resp = Request.CreateResponse(HttpStatusCode.OK);
            resp.Content = new StringContent(
                JsonConvert.SerializeObject(
                    GetAllWhere(a => !a.SoftDeleted && (a.Author.Id == creatorId && ((a.IsPublic && a.Approved) || creatorId == userId))),
                    new JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 5
                    }), Encoding.UTF8, "application/json");

            await MakeLog(new Dictionary<string, string>() { { "creatorId", creatorId.ToString() }});

            return resp;
        }

        // GET: api/LearningActivities/5
        [ResponseType(typeof(LearningActivity))]
        public async Task<HttpResponseMessage> GetLearningActivity(int id)
        {
            Common.Models.LimitedLearningActivity limitedVersion = GetAllWhere(a => a.Id == id).FirstOrDefault();
            if (limitedVersion == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Not found");
            }

            var resp = Request.CreateResponse(HttpStatusCode.OK);
            resp.Content = new StringContent(JsonConvert.SerializeObject(limitedVersion, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Serialize, MaxDepth = 5 }), Encoding.UTF8, "application/json");

            await MakeLog(new Dictionary<string, string>() { { "id", id.ToString() } });

            return resp;
        }

        // GET: api/LearningActivities/WithCode?code=ABCDEF
        [ResponseType(typeof(LearningActivity))]
        public async Task<HttpResponseMessage> GetWithCode(string code)
        {
            if (code == "SALTWELLSTATUE") code = "CHARLTONSTATUE";

            Common.Models.LimitedLearningActivity limitedVersion = GetAllWhere(a => a.InviteCode == code.ToUpper()).FirstOrDefault();
            if (limitedVersion == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Not found");
            }

            var resp = Request.CreateResponse(HttpStatusCode.OK);
            resp.Content = new StringContent(JsonConvert.SerializeObject(limitedVersion, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Serialize, MaxDepth = 5 }), Encoding.UTF8, "application/json");

            await MakeLog(new Dictionary<string, string>() { { "code", code } });

            return resp;
        }

        // POST: api/LearningActivities/Approve?id=1
        [ResponseType(typeof(LearningActivity))]
        public async Task<HttpResponseMessage> Approve(int id)
        {
            ApplicationUser thisUser = await GetUser();

            if (thisUser == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Please log in");
            }

            if (!thisUser.Trusted)
            {
                return Request.CreateErrorResponse(HttpStatusCode.Forbidden, "You do not have permission to approve activities");
            }

            await MakeLog(new Dictionary<string, string>() { { "id", id.ToString() } });

            LearningActivity activity = await db.LearningActivities.FindAsync(id);

            if(activity != null)
            {
                activity.Approved = true;
                await db.SaveChangesAsync();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }


        // PUT: api/LearningActivities/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutLearningActivity(int id, LearningActivity learningActivity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            LearningActivity existing = db.LearningActivities.FirstOrDefault(a => a.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            ApplicationUser thisUser = await GetUser();
            if (thisUser == null || thisUser.Id != existing.Author.Id)
            {
                return Unauthorized();
            }
                
            existing.AppVersionNumber = learningActivity.AppVersionNumber;
            existing.Approved = thisUser.Trusted;
            existing.CreatedAt = DateTime.UtcNow;
            existing.Description = learningActivity.Description;
            existing.ImageUrl = learningActivity.ImageUrl;
            existing.Name = learningActivity.Name;
            existing.RequireUsername = learningActivity.RequireUsername;
            existing.IsPublic = learningActivity.IsPublic;
            existing.Places = await ProcessPlaces(learningActivity.Places, thisUser);
            existing.LearningTasks = await ProcessTasks(learningActivity, thisUser, true);

            db.Entry(existing).State = EntityState.Modified;

            await db.SaveChangesAsync();

            return StatusCode(HttpStatusCode.OK);
        }

        private async Task<List<Place>> ProcessPlaces(ICollection<Place> places, ApplicationUser currentUser)
        {
            // Go through the activity's Places, adding them to the database if necessary
            List<Place> finalPlaces = new List<Place>();
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

        private async Task<LearningTask> AddTaskIfNeeded(LearningTask thisTask, ApplicationUser currentUser, bool checkUpdated = false)
        {
            if (checkUpdated)
            {
                LearningTask existingTask = await db.LearningActivityTasks.FirstOrDefaultAsync(t => t.Id == thisTask.Id);

                if (existingTask != null)
                {
                    if (existingTask.Description == thisTask.Description &&
                        existingTask.ImageUrl == thisTask.ImageUrl &&
                        existingTask.JsonData == thisTask.JsonData &&
                        existingTask.Order == thisTask.Order)
                    {
                        // nothing has changed
                        return existingTask;
                    }

                    // else something has changed - make a new task so that existing responses don't break
                    existingTask.SoftDeleted = true;
                }
            }

            LearningTask dbTask = db.LearningActivityTasks.Add(new LearningTask
            {
                Author = currentUser,
                Description = thisTask.Description,
                JsonData = thisTask.JsonData,
                TaskType = await db.TaskTypes.SingleAsync(tt => tt.Id == thisTask.TaskType.Id),
                ParentTask = thisTask.ParentTask,
                Order = thisTask.Order
            });

            if (dbTask.TaskType.IdName == "SCAN_QR")
            {
                await db.SaveChangesAsync();
                dbTask.JsonData = await GenerateQR($"task-{dbTask.Id}", Common.ServerUtils.GetTaskQRCodeData(dbTask.Id));
            }

            await db.SaveChangesAsync();

            return dbTask;
        }

        private async Task<List<LearningTask>> ProcessTasks(LearningActivity learningActivity, ApplicationUser currentUser, bool checkUpdated = false)
        {
            //Add each task in the activity to the database
            //If a task has child tasks, add those first
            List<LearningTask> finalTasks = new List<LearningTask>();
            int orderCount = 0;

            for (int i = 0; i < learningActivity.LearningTasks.Count(); i++)
            {
                LearningTask thisTask = learningActivity.LearningTasks.ElementAt(i);
                thisTask.Order = orderCount++;

                LearningTask dbTask = await AddTaskIfNeeded(thisTask, currentUser, checkUpdated);

                List<LearningTask> finalChildTasks = new List<LearningTask>();

                if (thisTask.ChildTasks != null)
                {
                    foreach (LearningTask childTask in thisTask.ChildTasks)
                    {
                        childTask.ParentTask = dbTask;
                        finalChildTasks.Add(await AddTaskIfNeeded(childTask, currentUser, checkUpdated));
                    }
                }

                dbTask.ChildTasks = finalChildTasks;
                await db.SaveChangesAsync();
                finalTasks.Add(dbTask);
            }

            return finalTasks;
        }

        // POST: api/LearningActivities
        [ResponseType(typeof(LearningActivity))]
        public async Task<HttpResponseMessage> PostLearningActivity(LearningActivity learningActivity)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid LearningActivity");
            }

            ApplicationUser thisUser = await GetUser();

            if(thisUser == null)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Please log in");

            Application thisApp = db.Applications.AsEnumerable().FirstOrDefault();

            learningActivity.Places = await ProcessPlaces(learningActivity.Places, thisUser);

            learningActivity.LearningTasks = await ProcessTasks(learningActivity, thisUser);
            learningActivity.Author = thisUser;
            learningActivity.CreatedAt = DateTime.UtcNow;
            learningActivity.Approved = thisUser.Trusted;
            learningActivity.Application = thisApp;

            bool createdUnique = false;
            string randStr = "";
            while(!createdUnique)
            {
                randStr = new string(Enumerable.Repeat(chars, 6)
                  .Select(s => s[rand.Next(s.Length)]).ToArray());

                createdUnique = !await db.LearningActivities.AnyAsync(la => la.InviteCode == randStr);
            }

            // ~ 900k possible combinations. If this becomes an issue we're in a good place :)
            learningActivity.InviteCode = randStr;

            LearningActivity finalAct = db.LearningActivities.Add(learningActivity);
            foreach(Place p in learningActivity.Places)
            {
                p.Activities.Add(finalAct);
            }

            string qrCodeUrl = "qrCodes/" + finalAct.InviteCode + ".png";
            string shareAddress = ServerUtils.GetActivityShareUrl(finalAct.InviteCode);

            finalAct.QRCodeUrl = await GenerateQR(finalAct.InviteCode, shareAddress);

            await db.SaveChangesAsync();

            Common.Models.LimitedLearningActivity limitedVersion = GetAllWhere(a => a.Id == finalAct.Id).FirstOrDefault();

            await MakeLog(new Dictionary<string, string>() { { "id", finalAct.Id.ToString() } });

            var resp = Request.CreateResponse(HttpStatusCode.OK);
            resp.Content = new StringContent(JsonConvert.SerializeObject(limitedVersion, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");
            return resp;
        }

        private async Task<string> GenerateQR(string codeName, string codeData)
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

        // DELETE: api/LearningActivities/5
        [ResponseType(typeof(LearningActivity))]
        public async Task<HttpResponseMessage> DeleteLearningActivity(int id)
        {
            LearningActivity learningActivity = await db.LearningActivities.FindAsync(id);
            if (learningActivity == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid LearningActivity Id");
            }

            ApplicationUser thisUser = await GetUser();
            if (thisUser == null || !thisUser.Trusted && learningActivity.Author.Id != thisUser.Id)
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Access denied");
            }
 
            // Just add a 'deleted' flag so it doesn't show up anywhere,
            // in case users have downloaded + cached it offline
            learningActivity.SoftDeleted = true;            

            await db.SaveChangesAsync();

            await MakeLog(new Dictionary<string, string>() { { "id", id.ToString() } });

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool LearningActivityExists(int id)
        {
            return db.LearningActivities.Count(e => e.Id == id) > 0;
        }
    }
}