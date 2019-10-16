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
        public async Task<HttpResponseMessage> PostLearningActivity(ActivityCollection collection)
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
    }
}
