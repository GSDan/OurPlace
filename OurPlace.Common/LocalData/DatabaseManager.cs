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
using OurPlace.Common.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static OurPlace.Common.LocalData.Storage;
using static OurPlace.Common.Helpers;
using System.Threading.Tasks;

namespace OurPlace.Common.LocalData
{
    public class DatabaseManager
    {
        public ApplicationUser currentUser { get; set; }
        private IEnumerable<TaskType> taskTypes;
        private readonly SQLiteConnection connection;

        public DatabaseManager(string path)
        {
            try
            {
                connection = new SQLiteConnection(Path.Combine(path, "localData.db"));
                connection.CreateTable<ActivityProgress>();
                connection.CreateTable<TaskType>();
                connection.CreateTable<ApplicationUser>();
                connection.CreateTable<FileUpload>();
                connection.CreateTable<AppDataUpload>();
                connection.CreateTable<ActivityCache>();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void SaveActivityProgress(LearningActivity activity, ICollection<AppTask> progress, string enteredUsername)
        {
            List<AppTask> trimmed = new List<AppTask>();

            foreach (AppTask t in progress)
            {
                if (t != null) trimmed.Add(t);
            }

            ActivityProgress latestProg = new ActivityProgress
            {
                ActivityId = activity.Id,
                ActivityVersion = activity.ActivityVersionNumber,
                EnteredUsername = enteredUsername,
                JsonData = JsonConvert.SerializeObject(activity,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 5
                    }),
                AppTaskJson = JsonConvert.SerializeObject(trimmed,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 5
                    }),
            };

            AddProgress(latestProg);
            ShouldRefreshFeed = true;
        }

        public ApplicationUser GetUser()
        {
            currentUser = connection.Table<ApplicationUser>().FirstOrDefault();
            return currentUser;
        }

        public void CleanDatabase()
        {
            CleanAllButUser();

            try
            {
                connection.DeleteAll<ApplicationUser>();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            currentUser = null;
        }

        public void CleanAllButUser()
        {
            try
            {
                connection.DeleteAll<ActivityProgress>();
                connection.DeleteAll<TaskType>();
                connection.DeleteAll<FileUpload>();
                connection.DeleteAll<AppDataUpload>();
                connection.DeleteAll<ActivityCache>();
                connection.DeleteAll<LearningActivity>(); // this last, as might not exist
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void DeleteUser()
        {
            connection.DeleteAll<ApplicationUser>();
            currentUser = null;
        }

        public void AddUser(ApplicationUser data)
        {
            connection.DeleteAll<ApplicationUser>();
            connection.Insert(data);
            currentUser = data;
        }

        public void AddTaskTypes(ICollection<TaskType> types)
        {
            connection.DeleteAll<TaskType>();
            connection.InsertAll(types);
            taskTypes = types;
        }

        public IEnumerable<TaskType> GetTaskTypes()
        {
            if (taskTypes != null)
            {
                return taskTypes;
            }
            taskTypes = connection.Table<TaskType>().AsEnumerable().ToList();
            return taskTypes;
        }

        public IEnumerable<AppDataUpload> GetUploadQueue()
        {
            return connection.Table<AppDataUpload>().AsEnumerable().ToList();
        }

        public void AddUpload(AppDataUpload data)
        {
            connection.InsertOrReplace(data);
        }

        public void DeleteUpload(AppDataUpload data)
        {
            connection.Delete<AppDataUpload>(data.ItemId);
        }

        public void UpdateUpload(AppDataUpload data)
        {
            connection.Update(data);
        }

        public void AddProgress(ActivityProgress data)
        {
            connection.InsertOrReplace(data);
        }

        public void DeleteProgress(ActivityProgress data)
        {
            connection.Delete<ActivityProgress>(data.ActivityId);
        }

        public void DeleteProgress(int activityId)
        {
            connection.Delete<ActivityProgress>(activityId);
        }

        public ActivityProgress GetProgress(LearningActivity activity)
        {
            return connection.Table<ActivityProgress>().FirstOrDefault(prog => prog.ActivityId == activity.Id &&
                                                                               prog.ActivityVersion == activity.ActivityVersionNumber);
        }

        public List<ActivityProgress> GetProgress()
        {
            return connection.Table<ActivityProgress>().AsEnumerable().ToList();
        }

        // Cached activities
        public void AddActivity(LearningActivity act, int maxCacheCount = 4)
        {
            bool exists = connection.Table<ActivityCache>().Where(a => a.ActivityId == act.Id).Any();

            // Limit to 4 recent activities, delete oldest
            if(!exists)
            {
                int count = connection.Table<ActivityCache>().Count();
                if(count >= maxCacheCount)
                {
                    List<ActivityCache> cached = connection.Table<ActivityCache>().OrderBy(a => a.AddedAt).ToList();
                    while (cached.Count >= maxCacheCount)
                    {
                        LearningActivity thisAct = JsonConvert.DeserializeObject<LearningActivity>(cached.First().JsonData);
                        DeleteCachedActivity(thisAct);
                        cached.RemoveAt(0);
                    }
                }
            }

            connection.InsertOrReplace(new ActivityCache
            {
                ActivityId = act.Id,
                JsonData = JsonConvert.SerializeObject(act, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    MaxDepth = 5
                }),
                AddedAt = DateTime.UtcNow
            });
        }

        public void DeleteLearnerCacheAndProgress()
        {
            List<ActivityProgress> allProgress = GetProgress();
            foreach(ActivityProgress prog in allProgress)
            {
                // Delete any created response files 
                List<AppTask> tasks = JsonConvert.DeserializeObject<List<AppTask>>(prog.AppTaskJson);
                foreach(AppTask task in tasks)
                {
                    DeleteLearnerProgress(task);
                }
           
                DeleteProgress(prog.ActivityId);
            }

            List<ActivityCache> allCached = connection.Table<ActivityCache>().ToList();
            foreach(ActivityCache cache in allCached)
            {
                DeleteCachedActivity(JsonConvert.DeserializeObject<LearningActivity>(cache.JsonData));
            }

            ShouldRefreshFeed = true;
        }

        public List<LearningActivity> GetActivities()
        {
            List<ActivityCache> found = connection.Table<ActivityCache>()
                .OrderByDescending(a => a.AddedAt).AsEnumerable().ToList();

            List<LearningActivity> toRet = new List<LearningActivity>();

            if(found != null)
            {
                foreach(ActivityCache c in found)
                {
                    try
                    {
                        toRet.Add(JsonConvert.DeserializeObject<LearningActivity>(c.JsonData));
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            return toRet;
        }

        public LearningActivity GetActivity(int givenId)
        {
            ActivityCache found =  connection.Table<ActivityCache>().Where(act => act.ActivityId == givenId).FirstOrDefault();

            if (found == null) return null;

            return JsonConvert.DeserializeObject<LearningActivity>(found.JsonData);
        }

        public void DeleteCachedActivity(LearningActivity act)
        {
            DeleteActivityFileCache(act);
            connection.Delete<ActivityCache>(act.Id);
        }
    }
}