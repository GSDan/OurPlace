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
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace OurPlace.API.Controllers
{
    public class TaskTypesController : ParkLearnAPIController
    {
        // GET: api/TaskTypes
        public async Task<IQueryable<TaskType>> GetTaskTypes()
        {
            ApplicationUser thisUser = await GetUser();
            await MakeLog();

            return db.TaskTypes.Where(taskType =>  taskType.IdName != "SCAN_QR" );
        }

        // GET: api/TaskTypes/5
        [ResponseType(typeof(TaskType))]
        public async Task<IHttpActionResult> GetTaskType(int id)
        {
            TaskType taskType = await db.TaskTypes.FindAsync(id);
            if (taskType == null)
            {
                return NotFound();
            }

            return Ok(taskType);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool TaskTypeExists(int id)
        {
            return db.TaskTypes.Count(e => e.Id == id) > 0;
        }
    }
}