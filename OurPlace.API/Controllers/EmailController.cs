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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Results;
using Newtonsoft.Json;
using OurPlace.API.Models;
using OurPlace.Common;
using SendGrid;

namespace OurPlace.API.Controllers
{
    public class EmailController : ParkLearnAPIController
    {
        public struct NewEmailReq
        {
            public string[] ToAddresses { get; set; }
            public bool SendToAllUsers { get; set; }
            public string Subject { get; set; }
            public string Content { get; set; }
            public bool IsHTML { get; set; }
        }

        // POST: api/Email/SendNew
        [ResponseType(typeof(Response))]
        public async Task<IHttpActionResult> SendNew(NewEmailReq data)
        {
            ApplicationUser thisUser = await GetUser();
            if (thisUser == null || !ConfidentialData.TestEmails.Contains(thisUser.Email))
            {
                return Unauthorized();
            }

            if (data.SendToAllUsers)
            {
                data.ToAddresses = db.Users.Where(u => !string.IsNullOrEmpty(u.Email)).Select(u => u.Email).ToArray();
            }

            if(data.ToAddresses == null || data.ToAddresses.Length == 0)
            {
                return new ExceptionResult(new Exception("No addresses given"), this);
            }

            await MakeLog(new Dictionary<string, string> { { "data", JsonConvert.SerializeObject(data) } });

            Response resp = await ServerUtils.SendEmail(data.ToAddresses, data.Subject, data.Content, data.IsHTML);

            return Ok(resp);
        }


    }
}
