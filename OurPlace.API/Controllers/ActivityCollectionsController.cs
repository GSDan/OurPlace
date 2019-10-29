using OurPlace.API.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace OurPlace.API.Controllers
{
    public class ActivityCollectionsController : ParkLearnAPIController
    {
        // POST: api/ActivityCollections
        [ResponseType(typeof(LearningActivity))]
        public async Task<HttpResponseMessage> PostActivityCollection(ActivityCollection collection)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid LearningActivity");
            }

            ApplicationUser thisUser = await GetUser();

            if (thisUser == null)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Please log in");

            Application thisApp = db.Applications.AsEnumerable().FirstOrDefault();

            collection.Places = await ProcessPlacesInNewContent(collection.Places, thisUser);
            collection.Activities = await ProcessActivities(collection.Activities, thisUser);
            collection.Author = thisUser;
            collection.CreatedAt = DateTime.UtcNow;
            collection.Approved = thisUser.Trusted;
            collection.Application = thisApp;

            bool createdUnique = false;
            string randStr = "";
            while (!createdUnique)
            {
                randStr = new string(Enumerable.Repeat(chars, 6)
                  .Select(s => s[rand.Next(s.Length)]).ToArray());

                createdUnique = !await db.ActivityCollections.AnyAsync(la => la.InviteCode == randStr);
            }

            // ~ 900k possible combinations. If this becomes an issue we're in a good place :)
            collection.InviteCode = randStr;

            ActivityCollection finalAct = db.ActivityCollections.Add(collection);
            

            string qrCodeUrl = "qrCodes/" + finalAct.InviteCode + ".png";
            string shareAddress = ServerUtils.GetActivityShareUrl(finalAct.InviteCode);

            finalAct.QRCodeUrl = await GenerateQR(finalAct.InviteCode, shareAddress);

            await db.SaveChangesAsync();

            await MakeLog(new Dictionary<string, string>() { { "id", finalAct.Id.ToString() } });

            return Request.CreateResponse(HttpStatusCode.OK); ;
        }

        // PUT: api/LearningActivities/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutActivityCollection(int id, ActivityCollection collection)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ActivityCollection existing = db.ActivityCollections.FirstOrDefault(a => a.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            ApplicationUser thisUser = await GetUser();
            if (thisUser == null || thisUser.Id != existing.Author.Id)
            {
                return Unauthorized();
            }

            List<Place> places = await ProcessPlacesInNewContent(collection.Places, thisUser, existing.Places.ToList());
            List<LearningActivity> activities = (await ProcessActivities(collection.Activities, thisUser)).ToList();

            existing.CollectionVersionNumber = collection.CollectionVersionNumber;
            existing.Approved = thisUser.Trusted;
            existing.CreatedAt = DateTime.UtcNow;
            existing.Description = collection.Description;
            existing.ImageUrl = collection.ImageUrl;
            existing.Name = collection.Name;
            existing.IsPublic = collection.IsPublic;
            existing.Places = places;
            existing.Activities = activities;
            existing.ActivityOrder = collection.ActivityOrder;

            db.Entry(existing).State = System.Data.Entity.EntityState.Modified;

            await db.SaveChangesAsync();

            return StatusCode(HttpStatusCode.OK);
        }

        private async Task<ICollection<LearningActivity>> ProcessActivities(ICollection<LearningActivity> activities, ApplicationUser thisUser)
        {
            List<LearningActivity> finalActs = new List<LearningActivity>();

            if (activities == null) return finalActs;

            foreach (LearningActivity act in activities)
            {
                LearningActivity existing = await db.LearningActivities.Where(a => a.Id == act.Id).FirstOrDefaultAsync();
                if(existing != null)
                {
                    finalActs.Add(existing);
                }
            }

            return finalActs;
        }

        // DELETE: api/ActivityCollections/5
        [ResponseType(typeof(ActivityCollection))]
        public async Task<HttpResponseMessage> DeleteActivityCollection(int id)
        {
            ActivityCollection collection = await db.ActivityCollections.FindAsync(id);
            if (collection == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid Id");
            }

            ApplicationUser thisUser = await GetUser();
            if (thisUser == null || !thisUser.Trusted && collection.Author.Id != thisUser.Id)
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Access denied");
            }

            // Just add a 'deleted' flag so it doesn't show up anywhere,
            // in case users have downloaded + cached it offline
            collection.SoftDeleted = true;

            await db.SaveChangesAsync();

            await MakeLog(new Dictionary<string, string>() { { "id", id.ToString() } });

            return Request.CreateResponse(HttpStatusCode.OK);
        }

    }
}
