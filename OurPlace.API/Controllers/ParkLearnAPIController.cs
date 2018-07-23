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
using Newtonsoft.Json;
using OurPlace.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace OurPlace.API.Controllers
{
    public class ParkLearnAPIController : ApiController
    {
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
    }
}