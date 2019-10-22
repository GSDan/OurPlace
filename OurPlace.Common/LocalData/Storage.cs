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
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OurPlace.Common.LocalData
{
    public static class Storage
    {
        public class TaskFileInfo
        {
            public string fileUrl;
            public string extension;
        }

        private static string fileSystem;
        private static string fileCacheFolder;
        private static DatabaseManager dbManager;

        public static bool ShouldRefreshFeed = true;

        public static async Task<DatabaseManager> GetDatabaseManager(bool runLogin = true)
        {
            InitializeFolders();
            if (dbManager != null) return dbManager;
            dbManager = new DatabaseManager(fileCacheFolder);
            if (runLogin) await InitializeLogin(dbManager);
            return dbManager;
        }

        /// <summary>
        /// Initialize the app's folders
        /// </summary>
        private static void InitializeFolders()
        {
            fileSystem = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            fileCacheFolder = Path.Combine(fileSystem, "cache");

            if (!Directory.Exists(fileCacheFolder))
            {
                Directory.CreateDirectory(fileCacheFolder);
            }
        }

        public static async Task<bool> InitializeLogin(DatabaseManager passedManager = null)
        {
            if (passedManager == null) passedManager = await GetDatabaseManager(false);

            // If the database doesn't contain a valid user, prompt sign in
            ApplicationUser foundUser = passedManager.GetUser();
            if (foundUser == null
                || string.IsNullOrWhiteSpace(foundUser.RefreshToken)
                || foundUser.RefreshExpiresAt == null
                || foundUser.RefreshExpiresAt < DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }

        public static List<TaskFileInfo> GetFileTasks(LearningActivity act)
        {
            List<TaskFileInfo> toRet = new List<TaskFileInfo>();

            if (act.LearningTasks == null) return toRet;

            foreach (LearningTask t in act.LearningTasks)
            {
                try
                {
                    TaskFileInfo parentInfo = GetInfoForTask(t);
                    if (parentInfo != null) toRet.Add(parentInfo);

                    foreach (LearningTask child in t.ChildTasks)
                    {
                        TaskFileInfo childInfo = GetInfoForTask(child);
                        if (childInfo != null) toRet.Add(childInfo);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return toRet;
        }

        private static TaskFileInfo GetInfoForTask(LearningTask task)
        {
            string tt = task.TaskType?.IdName;
            if (tt == "MATCH_PHOTO" || tt == "LISTEN_AUDIO" ||
                (tt == "DRAW_PHOTO" && !task.JsonData.StartsWith("TASK::", StringComparison.OrdinalIgnoreCase)))
            {
                return new TaskFileInfo { extension = ServerUtils.GetFileExtension(task.TaskType.IdName), fileUrl = task.JsonData };
            }
            else if (tt == "INFO")
            {
                AdditionalInfoData infoData = JsonConvert.DeserializeObject<AdditionalInfoData>(task.JsonData);
                if (!string.IsNullOrWhiteSpace(infoData.ImageUrl))
                {
                    return new TaskFileInfo { extension = ServerUtils.GetFileExtension(task.TaskType.IdName), fileUrl = infoData.ImageUrl };
                }
            }
            return null;
        }

        public static string GetCacheFolder(string subFolder = null)
        {
            if (!string.IsNullOrWhiteSpace(subFolder))
            {
                string thisPath = Path.Combine(fileCacheFolder, subFolder);

                if (!Directory.Exists(thisPath))
                {
                    Directory.CreateDirectory(thisPath);
                }

                return thisPath;
            }

            return fileCacheFolder;

        }

        public static string GetUploadsFolder()
        {
            return GetCacheFolder("uploads");
        }

        // Get the path where this remote file would be cached on the local device
        public static string GetCacheFilePath(string url, int activityId, string extension)
        {
            string folderPath = GetCacheFolder(activityId.ToString());
            string filename = Path.GetFileName(url) + "." + extension;

            return Path.Combine(folderPath, filename);
        }

        public static void DeleteActivityFileCache(FeedItem content)
        {
            if(content is LearningActivity act)
            {
                List<TaskFileInfo> fileUrls = new List<TaskFileInfo>();

                GetFileTasks(act);

                for (int i = 0; i < fileUrls.Count; i++)
                {
                    string thisUrl = ServerUtils.GetUploadUrl(fileUrls[i].fileUrl);
                    string cachedFilePath = GetCacheFilePath(thisUrl, content.Id, fileUrls[i].extension);

                    File.Delete(cachedFilePath);
                }
            }

            if (!string.IsNullOrWhiteSpace(content.ImageUrl))
            {
                string thisUrl = ServerUtils.GetUploadUrl(content.ImageUrl);
                string cachedFilePath = GetCacheFilePath(thisUrl, content.Id, Path.GetExtension(content.ImageUrl));

                File.Delete(cachedFilePath);
            }
        }

        public static void DeleteInProgress(LearningActivity act)
        {
            List<FileUpload> files = GetFilesForCreation(act);

            foreach (FileUpload file in files)
            {
                string filePath = Path.Combine(fileCacheFolder, file.LocalFilePath);
                File.Delete(filePath);
            }
        }

        public static string WriteDataToFile(string path, byte[] data)
        {
            string parentDir = Directory.GetParent(path).FullName;

            Directory.CreateDirectory(parentDir);

            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(data, 0, data.Length);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

            return path;
        }

        /// <summary>
        /// Move the given task's files to the upload staging folder
        /// and update it accordingly
        /// </summary>
        public static AppTask PrepForUpload(AppTask given, bool addCache = false)
        {
            given.CompletionData.FinishedAt = DateTime.UtcNow;

            string cache = GetCacheFolder();

            string[] theseFiles = GetPathsIfFiles(given);
            List<string> newFiles = new List<string>();

            if (theseFiles != null)
            {
                foreach (string f in theseFiles)
                {
                    string fullPath = addCache ? Path.Combine(cache, f) : f;

                    if (!File.Exists(fullPath))
                    {
                        continue;
                    }

                    string filename = Path.GetFileName(fullPath);

                    // Move file to uploads folder
                    string uploadsDest = Path.Combine(GetUploadsFolder(), filename);

                    File.Move(fullPath, uploadsDest);

                    newFiles.Add(filename);
                }

                given.CompletionData.JsonData = JsonConvert.SerializeObject(newFiles);
            }

            return given;
        }

        public static void DeleteLearnerProgress(AppTask given, bool addCache = false)
        {
            string cache = GetCacheFolder();
            string[] theseFiles = GetPathsIfFiles(given);

            if (theseFiles != null)
            {
                foreach (string file in theseFiles)
                {
                    string fullPath = addCache ? Path.Combine(cache, file) : file;

                    if (!File.Exists(fullPath))
                    {
                        continue;
                    }

                    File.Delete(fullPath);
                }
            }
        }

        private static string[] GetPathsIfFiles(AppTask t)
        {
            string[] theseFiles = null;
            if (t.TaskType.ReqFileUpload)
            {
                theseFiles = JsonConvert.DeserializeObject<string[]>(t.CompletionData.JsonData);
            }

            return theseFiles;
        }

        public static void CleanCache()
        {
            string[] dirs = Directory.GetDirectories(fileCacheFolder);

            foreach (string folder in dirs)
            {
                Console.WriteLine("Deleting " + folder);
                Directory.Delete(folder, true);
            }

            dbManager.CleanAllButUser();
        }

        public static List<FileUpload> MakeUploads(List<AppTask> items)
        {
            List<FileUpload> uploads = new List<FileUpload>();
            foreach (AppTask t in items)
            {
                if (t == null) continue;
                string[] theseFiles = GetPathsIfFiles(t);
                if (theseFiles != null)
                {
                    foreach (string file in theseFiles)
                    {
                        string uploadCache = Path.Combine(GetUploadsFolder(), file);

                        if (!File.Exists(uploadCache))
                        {
                            continue;
                        }

                        uploads.Add(new FileUpload
                        {
                            LocalFilePath = file
                        });
                    }
                }
            }
            return uploads;
        }

        public static async Task<AppDataUpload> PrepCreationForUpload(FeedItem created, bool updatingRemote)
        {
            created.AppVersionNumber = Helpers.AppVersionNumber;

            AppDataUpload uploadData = new AppDataUpload
            {
                ItemId = created.Id,
                Name = created.Name,
                CreatedAt = created.CreatedAt,
                Description = created.Description,
                ImageUrl = created.ImageUrl,
                JsonData = JsonConvert.SerializeObject(created,
                    new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 5
                    }),
                FilesJson = JsonConvert.SerializeObject( GetFilesForCreation(created))
            };

            DatabaseManager databaseManager = await GetDatabaseManager();
            
            // Remove from cache when added to upload queue
            if(created is LearningActivity createdActivity)
            {
                uploadData.UploadType = (updatingRemote) ? UploadType.UpdatedActivity : UploadType.NewActivity;
                uploadData.UploadRoute = (updatingRemote) ? "api/learningactivities?id=" + created.Id : "api/learningactivities";

                string cacheJson = databaseManager.CurrentUser.LocalCreatedActivitiesJson;
                List<LearningActivity> inProgress = (string.IsNullOrWhiteSpace(cacheJson)) ?
                    new List<LearningActivity>() :
                    JsonConvert.DeserializeObject<List<LearningActivity>>(cacheJson);

                int existingInd = inProgress.FindIndex((la) => la.Id == created.Id);
                if (existingInd != -1)
                {
                    inProgress.RemoveAt(existingInd);
                }

                databaseManager.CurrentUser.LocalCreatedActivitiesJson = JsonConvert.SerializeObject(inProgress);
            }
            else if(created is ActivityCollection createdCollection)
            {
                uploadData.UploadType = (updatingRemote) ? UploadType.UpdatedCollection : UploadType.NewCollection;
                uploadData.UploadRoute = (updatingRemote) ? "api/activitycollections?id=" + created.Id : "api/activitycollections";

                string cacheJson = databaseManager.CurrentUser.LocalCreatedCollectionsJson;
                List<ActivityCollection> inProgress = (string.IsNullOrWhiteSpace(cacheJson)) ?
                    new List<ActivityCollection>() :
                    JsonConvert.DeserializeObject<List<ActivityCollection>>(cacheJson);

                int existingInd = inProgress.FindIndex((la) => la.Id == created.Id);
                if (existingInd != -1)
                {
                    inProgress.RemoveAt(existingInd);
                }

                databaseManager.CurrentUser.LocalCreatedCollectionsJson = JsonConvert.SerializeObject(inProgress);
            }

            databaseManager.AddUpload(uploadData);
            databaseManager.AddUser(databaseManager.CurrentUser);

            return uploadData;
        }

        private static List<FileUpload> GetFilesForCreation(FeedItem created)
        {
            List<FileUpload> files = new List<FileUpload>();

            if(created is LearningActivity createdActivity)
            {
                if (createdActivity.LearningTasks != null)
                {
                    foreach (LearningTask t in createdActivity.LearningTasks)
                    {
                        string thisFile = GetPathIfFile(t);
                        if (!string.IsNullOrWhiteSpace(thisFile))
                        {
                            files.Add(new FileUpload
                            {
                                LocalFilePath = thisFile
                            });
                        }
                        foreach (LearningTask c in t.ChildTasks)
                        {
                            thisFile = GetPathIfFile(c);
                            if (!string.IsNullOrWhiteSpace(thisFile))
                            {
                                files.Add(new FileUpload
                                {
                                    LocalFilePath = thisFile
                                });
                            }
                        }
                    }
                }
            }
            
            if (!string.IsNullOrWhiteSpace(created.ImageUrl) && !created.ImageUrl.StartsWith("upload"))
            {
                files.Add(new FileUpload
                {
                    LocalFilePath = created.ImageUrl
                });
            }

            return files;
        }

        private static string GetPathIfFile(LearningTask t)
        {
            string thisFile = null;
            switch (t.TaskType.IdName)
            {
                case "MATCH_PHOTO":
                case "LISTEN_AUDIO":
                    thisFile = t.JsonData;
                    break;
                case "DRAW_PHOTO":
                    if (!t.JsonData.StartsWith("TASK::", StringComparison.InvariantCulture))
                    {
                        thisFile = t.JsonData;
                    }
                    break;
                case "INFO":
                    AdditionalInfoData data = JsonConvert.DeserializeObject<AdditionalInfoData>(t.JsonData);
                    if (!string.IsNullOrWhiteSpace(data.ImageUrl)) thisFile = data.ImageUrl;
                    break;
            }

            return thisFile == null || thisFile.StartsWith("upload") ? null : thisFile;
        }

        /// <summary>
        /// Uploads the given files. Throws an exception if the user needs to log in again
        /// </summary>
        /// <returns>Success boolean</returns>
        public static async Task<bool> UploadFiles(List<FileUpload> files, int uploadListPos, Action<int> percentComplete, Action<int, string> updateUpload, string folderLoc)
        {
            bool err = false;
            bool tokenIssue = false;

            if (files.Count > 0)
            {
                long uploaded = 0;
                long totalSize = 0;

                // get total upload size
                foreach (FileUpload up in files)
                {
                    if (!string.IsNullOrWhiteSpace(up.RemoteFilePath))
                    {
                        continue;
                    }

                    string absPath = Path.Combine(folderLoc, up.LocalFilePath);

                    if (!File.Exists(absPath))
                    {
                        continue;
                    }

                    totalSize += new FileInfo(absPath).Length;
                }

                foreach (FileUpload up in files)
                {
                    if (!string.IsNullOrWhiteSpace(up.RemoteFilePath))
                    {
                        continue;
                    }

                    string absPath = Path.Combine(folderLoc, up.LocalFilePath);

                    if (!File.Exists(absPath))
                    {
                        continue;
                    }

                    try
                    {
                        string filename = Path.GetFileName(absPath);

                        long thisFileSize = new FileInfo(absPath).Length;

                        ServerResponse<string> resp;

                        using (var stream = File.OpenRead(absPath))
                        {
                            resp = await ServerUtils.UploadFile<string>(
                                "api/upload",
                                filename,
                                stream);
                        }


                        if (resp == null)
                        {
                            tokenIssue = true;
                            break;
                        }

                        if (resp.Success)
                        {
                            up.RemoteFilePath = resp.Data;
                            updateUpload(uploadListPos, JsonConvert.SerializeObject(files));

                            File.Delete(absPath);

                            uploaded += thisFileSize;
                            percentComplete((int)(uploaded / totalSize * 100));
                        }
                        else
                        {
                            err = true;
                            break;
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        err = true;
                    }
                }
            }

            if (tokenIssue)
            {
                throw new Exception("Refresh token invalid, please sign in again");
            }

            return !err;
        }

    }
}