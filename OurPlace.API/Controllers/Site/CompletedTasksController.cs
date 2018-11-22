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
using Microsoft.AspNet.Identity.Owin;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using OurPlace.API.Models;
using OurPlace.Common;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static OurPlace.API.ServerUtils;

namespace OurPlace.API.Controllers.Site
{
    [Authorize]
    [RequireHttps]
    public class CompletedTasksController : OurPlaceSiteController
    {
        private List<string> imageTasks;
        private List<string> linkTasks;

        public CompletedTasksController()
        {
            imageTasks = new List<string> { "TAKE_PHOTO", "MATCH_PHOTO", "DRAW", "DRAW_PHOTO" };
            linkTasks = new List<string> { "TAKE_VIDEO", "REC_AUDIO" };
            linkTasks.AddRange(imageTasks);
        }

        // GET: CompletedTasks
        [AllowAnonymous]
        public async Task<ActionResult> Index(int? submissionId, string code)
        {
            CompletedActivity activity;
            ApplicationUser thisUser = null;

            if (code != null)
            {
                activity = await db.CompletedActivities.Where(act => act.Share.ShareCode == code).FirstOrDefaultAsync();
                if(activity == null) return new HttpNotFoundResult();
            }
            else if (User == null || User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                thisUser = await UserManager.FindByNameAsync(User.Identity.Name);
                activity = await db.CompletedActivities.Where(act => act.Id == submissionId).FirstOrDefaultAsync();
                if (activity == null) return new HttpNotFoundResult();
                if (thisUser == null || activity.User.Id != thisUser.Id &&
                    !(activity.ShareWithCreator && thisUser.Id == activity.LearningActivity.Author.Id))
                    return new HttpUnauthorizedResult();
            }

            if(thisUser == null || activity.User.Id != thisUser.Id)
            {
                ViewData["title"] = string.Format("'{0}', uploaded by {1} on {2}", 
                    activity.LearningActivity.Name, 
                    activity.User.FirstName + " " + activity.User.Surname,
                    activity.CreatedAt.ToString(@"dd\/MM\/yyyy"));
            }
            else
            {
                ViewData["title"] = string.Format("'{0}', uploaded on {1}", 
                    activity.LearningActivity.Name, 
                    activity.CreatedAt.ToString(@"dd\/MM\/yyyy"));
            }

            await MakeLog(new Dictionary<string, string>() {
                { "submissionId", submissionId.ToString() },
                { "code", code }
            });

            ViewData["data"] = await db.CompletedTasks.Where(ct => ct.ParentSubmission.Id == activity.Id).OrderBy(ct => ct.EventTask.Order).ToListAsync();
            ViewData["submissionId"] = activity.Id;        
            ViewData["storage"] = ConfidentialData.storage;
            ViewData["imageTasks"] = imageTasks;
            ViewData["linkTasks"] = linkTasks;
            ViewData["MapsKey"] = ConfidentialData.mapsk;
            ViewData["Username"] = activity.EnteredUsername;
            return View();
        }

        // GET: CompletedTasks/Download?submissionId=0
        public async Task Download(int submissionId)
        {
            CompletedActivity activity = db.CompletedActivities.FirstOrDefault(act => act.Id == submissionId);
            if (activity == null) return;

            CloudBlobContainer container = GetCloudBlobContainer();

            List<CompletedTask> tasks = db.CompletedTasks.Where(ct => ct.ParentSubmission.Id == submissionId).ToList();

            List<DownloadStruct> toDl = new List<DownloadStruct>();
            foreach (CompletedTask task in tasks)
            {
                string idName = task.EventTask.TaskType.IdName;
                if (!linkTasks.Contains(idName)) continue;

                string[] links = JsonConvert.DeserializeObject<string[]>(task.JsonData);
                if (links == null) continue;

                for (int i = 0; i < links.Length; i++)
                {
                    toDl.Add(new DownloadStruct
                    {
                        Blob = container.GetBlockBlobReference(links[i]),
                        Filename =
                            $"{task.Id}_{task.EventTask.TaskType.IdName}-{i:00}.{Common.ServerUtils.GetFileExtension(idName)}"
                    });
                }
            }

            await MakeLog(new Dictionary<string, string>() { { "submissionId", submissionId.ToString() } });

            ZipFilesToResponse(Response, $"OurPlace-{submissionId}", toDl);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
