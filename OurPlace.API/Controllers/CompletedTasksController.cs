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
using OurPlace.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace OurPlace.API.Controllers
{
    public class CompletedTasksController : ParkLearnAPIController
    {
        public CompletedTasksController()
        {
        }

        // GET: api/CompletedTasks
        public IQueryable<CompletedTask> GetCompletedTasks()
        {
            return db.CompletedTasks;
        }

        // GET: api/CompletedTasks/5
        [ResponseType(typeof(CompletedTask))]
        public async Task<IHttpActionResult> GetCompletedTask(int id)
        {
            CompletedTask completedTask = await db.CompletedTasks.FindAsync(id);
            if (completedTask == null)
            {
                return NotFound();
            }

            return Ok(completedTask);
        }

        // POST: api/CompletedTasks/Submit
        [ResponseType(typeof(CompletedTask))]
        public async Task<IHttpActionResult> Submit(int activityId, Common.Models.AppTask[] results, bool shareWithCreator = false, string enteredName = null)
        {
            LearningActivity act = await db.LearningActivities.FindAsync(activityId);
            if (act == null)
            {
                return NotFound();
            }

            ApplicationUser thisUser = await GetUser();
            if (thisUser == null)
            {
                return Unauthorized();
            }

            // Generate share code
            Guid g = Guid.NewGuid();
            string guid = Convert.ToBase64String(g.ToByteArray());
            guid = guid.Replace("=", "");
            guid = guid.Replace("+", "");
            guid = guid.Replace("/", "");
            guid = guid.Substring(0, 12);

            CompletedActivity newSubmission = new CompletedActivity
            {
                CreatedAt = DateTime.UtcNow,
                LearningActivity = act,
                User = thisUser,
                ShareWithCreator = shareWithCreator,
                EnteredUsername = enteredName,
                Share = new UploadShare
                {
                    CreatedAt = DateTime.UtcNow,
                    Active = true,
                    ShareCode = guid
                }
            };
            CompletedActivity inData = db.CompletedActivities.Add(newSubmission);

            foreach (Common.Models.AppTask t in results)
            {
                CompletedTask newResult = new CompletedTask
                {
                    CreatedAt = DateTime.UtcNow,
                    JsonData = t.CompletionData.JsonData,
                    EventTask = await db.LearningActivityTasks.FindAsync(t.Id),
                    ParentSubmission = newSubmission
                };
                db.CompletedTasks.Add(newResult);
            }

            await db.SaveChangesAsync();

            await MakeLog(new Dictionary<string, string> { { "id", inData.Id.ToString() } });

            return Ok();
        }

        // DELETE: api/CompletedTasks/5
        [ResponseType(typeof(CompletedTask))]
        public async Task<IHttpActionResult> DeleteCompletedTask(int id)
        {
            CompletedTask completedTask = await db.CompletedTasks.FindAsync(id);
            if (completedTask == null)
            {
                return NotFound();
            }

            db.CompletedTasks.Remove(completedTask);
            await db.SaveChangesAsync();

            return Ok(completedTask);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool CompletedTaskExists(int id)
        {
            return db.CompletedTasks.Count(e => e.Id == id) > 0;
        }
    }
}