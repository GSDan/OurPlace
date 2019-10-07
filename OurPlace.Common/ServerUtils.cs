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
using RestSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using OurPlace.Common.LocalData;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ParkLearn.PCL.Models;

namespace OurPlace.Common
{
    public class ServerResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public T Data { get; set; }
    }

    public static class ServerUtils
    {
        private static RestClient restClient;
        private static HttpClient uploadClient;

        private static readonly string ServerErr = "SERVER_ERROR";

        /// <summary>
        /// Gets an access token to use as a bearer in request headers. 
        /// Returns null if there isn't a valid access or refresh token.
        /// If a null is returned, the user should be logged out to re-authenticate.
        /// Returns ServerUtils.ServerErr if the request failed for other reasons.
        /// </summary>
        /// <returns>The access token.</returns>
        private static async Task<string> GetAccessToken()
        {
            DatabaseManager dbManager = await Storage.GetDatabaseManager(false);

            if (dbManager.CurrentUser == null)
            {
                return null;
            }

            // return access token if one exists and is still valid
            if (!string.IsNullOrWhiteSpace(dbManager.CurrentUser.AccessToken) &&
                dbManager.CurrentUser.AccessExpiresAt > DateTime.UtcNow)
            {
                return dbManager.CurrentUser.AccessToken;
            }

            // need to get new access token.
            // Check that we have a refresh token which exists and is valid
            if (!string.IsNullOrWhiteSpace(dbManager.CurrentUser.RefreshToken) &&
                dbManager.CurrentUser.RefreshExpiresAt > DateTime.UtcNow)
            {
                // request new access token
                try
                {
                    using (var client = new HttpClient())
                    {
                        Dictionary<string, string> args = new Dictionary<string, string>
                        {
                            { "grant_type", "refresh_token" },
                            { "refresh_token", dbManager.CurrentUser.RefreshToken }
                        };

                        HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, ConfidentialData.api + "Token")
                        {
                            Content = new FormUrlEncodedContent(args)
                        };
                        HttpResponseMessage resp = await client.SendAsync(req);


                        if (resp.Content == null || !resp.IsSuccessStatusCode)
                        {
                            // failed, check if the refresh token is invalid
                            if (resp.Content != null && resp.StatusCode == HttpStatusCode.BadRequest)
                            {
                                string json = await resp.Content.ReadAsStringAsync();

                                if (!string.IsNullOrWhiteSpace(json))
                                {
                                    ErrorResponse errResp = JsonConvert.DeserializeObject<ErrorResponse>(json);
                                    if (errResp != null && errResp.Error == "invalid_grant")
                                    {
                                        // token invalid, prompt new sign-in
                                        return null;
                                    }
                                }
                            }
                            // failed for reasons other than the token, shouldn't log out
                            return ServerErr;
                        }
                        else
                        {
                            string json = await resp.Content.ReadAsStringAsync();
                            RefreshResponse refResp = JsonConvert.DeserializeObject<RefreshResponse>(json);

                            if (refResp != null && !string.IsNullOrWhiteSpace(refResp.Access_token))
                            {
                                // success! Save the new access token and its expiry time
                                dbManager.CurrentUser.AccessToken = refResp.Access_token;
                                dbManager.CurrentUser.AccessExpiresAt = DateTime.UtcNow.AddSeconds(refResp.Expires_in);
                                dbManager.AddUser(dbManager.CurrentUser);
                                return refResp.Access_token;
                            }

                            return ServerErr;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return ServerErr;
                }
            }

            // returning null should prompt return to sign-in
            return null;
        }

        private static void Setup()
        {
            if (restClient == null)
            {
                restClient = new RestClient(ConfidentialData.api);
            }
        }

        public static string GetFileExtension(string taskIdName)
        {
            switch (taskIdName)
            {
                case "TAKE_PHOTO":
                case "MATCH_PHOTO":
                case "DRAW":
                case "DRAW_PHOTO":
                case "INFO":
                    return "jpg";
                case "TAKE_VIDEO":
                case "REC_AUDIO":
                case "LISTEN_AUDIO":
                    return "mp4";
                default:
                    // uhhhh wut
                    return "file";
            }
        }

        public static string GetUploadUrl(string data)
        {
            return ConfidentialData.storage + data;
        }

        private static async Task<ServerResponse<TaskType[]>> GetTaskTypes()
        {
            ServerResponse<TaskType[]> response = await Get<TaskType[]>("/api/tasktypes");
            return response;
        }

        public static async Task<List<TaskType>> RefreshTaskTypes(DatabaseManager dbManager = null)
        {
            if (dbManager == null) dbManager = await Storage.GetDatabaseManager();

            ServerResponse<TaskType[]> response = await GetTaskTypes();

            if (response == null)
            {
                // refresh token invalid - return null
                return null;
            }

            if (!response.Success)
            {
                // token fine, just a failed request - return empty
                return new List<TaskType>();
            }

            // TODO make sure to remove this after updating the iOS version!!

            List<TaskType> tempTypes = new List<TaskType>(response.Data)
            {
                new TaskType
                {
                    Id = 14,
                    Order = 10,
                    IdName = "SCAN_QR",
                    ReqFileUpload = false,
                    DisplayName = "Scan the QR Code",
                    Description = "Find and scan the correct QR code",
                    IconUrl = ConfidentialData.storage + "icons/scanQR.png"
                }
            };

            dbManager.AddTaskTypes(tempTypes);
            return tempTypes;
        }

        public static string GetTaskQRCodeData(int taskId)
        {
            return ConfidentialData.api + "app/task?id=" + taskId;
        }

        public static bool CheckNeedsLogin(HttpStatusCode status)
        {
            if (status == HttpStatusCode.Forbidden || status == HttpStatusCode.Unauthorized)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Performs a GET request to the given route
        /// </summary>
        /// <typeparam name="T">Data type expected to be returned within the response</typeparam>
        /// <param name="route">The controller route (with first slash)</param>
        /// <param name="reqAccessToken">If an access token should be sent</param>
        /// <returns>Server response with returned data. If return is null, user should be logged out</returns>
        public static async Task<ServerResponse<T>> Get<T>(string route, bool reqAccessToken = true)
        {
            try
            {
                Setup();

                var req = new RestRequest(route, Method.GET);

                if (reqAccessToken)
                {
                    string accessToken = await GetAccessToken();

                    // Access and refresh tokens invalid, prompt new sign-in
                    if (accessToken == null) return null;

                    if (accessToken == ServerErr)
                    {
                        throw new Exception("There was an issue contacting the server");
                    }

                    req.AddParameter("Authorization", string.Format("Bearer " + accessToken), ParameterType.HttpHeader);
                }

                IRestResponse resp = await restClient.ExecuteTaskAsync(req);

                ServerResponse<T> toRet = new ServerResponse<T>
                {
                    Success = resp.IsSuccessful,
                    StatusCode = resp.StatusCode,
                    Message = resp.StatusDescription
                };

                if (toRet.Success && typeof(T) != typeof(string))
                {
                    toRet.Data = JsonConvert.DeserializeObject<T>(resp.Content);
                }

                return toRet;
            }
            catch (Exception e)
            {
                return new ServerResponse<T>
                {
                    Success = false,
                    Message = e.Message,
                    StatusCode = HttpStatusCode.BadRequest
                };
            }
        }

        /// <summary>
        /// Performs a DELETE request to the given route
        /// </summary>
        /// <typeparam name="T">Data type expected to be returned within the response</typeparam>
        /// <param name="route">The controller route (with first slash)</param>
        /// <returns>Server response with returned data. If return is null, user should be logged out</returns>
        public static async Task<ServerResponse<T>> Delete<T>(string route)
        {
            try
            {
                Setup();
                var req = new RestRequest(route, Method.DELETE);

                string accessToken = await GetAccessToken();

                // Access and refresh tokens invalid, prompt new sign-in
                if (accessToken == null) return null;

                if (accessToken == ServerErr)
                {
                    throw new Exception("There was an issue contacting the server");
                }

                req.AddParameter("Authorization", string.Format("Bearer " + accessToken), ParameterType.HttpHeader);

                IRestResponse resp = await restClient.ExecuteTaskAsync(req);

                ServerResponse<T> toRet = new ServerResponse<T>
                {
                    Success = resp.IsSuccessful,
                    StatusCode = resp.StatusCode,
                    Message = resp.StatusDescription
                };

                if (toRet.Success && typeof(T) != typeof(string))
                {
                    toRet.Data = JsonConvert.DeserializeObject<T>(resp.Content);
                }

                return toRet;
            }
            catch (Exception e)
            {
                return new ServerResponse<T>
                {
                    Success = false,
                    Message = e.Message,
                    StatusCode = HttpStatusCode.BadRequest
                };
            }
        }

        /// <summary>
        /// Performs a POST request to the given route
        /// </summary>
        /// <typeparam name="T">Data type expected to be returned within the response</typeparam>
        /// <param name="route">The controller route (with first slash)</param>
        /// <param name="data">The data to be posted</param>
        /// <param name="reqAccessToken">If an access token should be sent</param>
        /// <returns>Server response with returned data. If return is null, user should be logged out</returns>
        public static async Task<ServerResponse<T>> Post<T>(string route, object data, bool reqAccessToken = true)
        {
            try
            {
                using (var client = new HttpClient())
                {

                    if (reqAccessToken)
                    {
                        string accessToken = await GetAccessToken();

                        // Access and refresh tokens invalid, prompt new sign-in
                        if (accessToken == null) return null;

                        if (accessToken == ServerErr)
                        {
                            throw new Exception("There was an issue contacting the server");
                        }

                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }

                    string jsonContent = JsonConvert.SerializeObject(data, new JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 5,
                        Formatting = Formatting.None
                    });

                    HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    HttpResponseMessage resp = await client.PostAsync(ConfidentialData.api + route, content);
                    string json = await resp.Content.ReadAsStringAsync();

                    ServerResponse<T> toRet = new ServerResponse<T>
                    {
                        Success = resp.IsSuccessStatusCode,
                        Message = resp.ReasonPhrase,
                        StatusCode = resp.StatusCode,
                    };

                    if (toRet.Success && typeof(T) != typeof(string))
                    {
                        toRet.Data = JsonConvert.DeserializeObject<T>(json);
                    }

                    return toRet;
                }
            }
            catch (Exception e)
            {
                return new ServerResponse<T>
                {
                    Success = false,
                    Message = e.Message,
                    StatusCode = HttpStatusCode.BadRequest
                };
            }
        }

        /// <summary>
        /// Performs a POST request to the given route
        /// </summary>
        /// <typeparam name="T">Data type expected to be returned within the response</typeparam>
        /// <param name="route">The controller route (with first slash)</param>
        /// <param name="data">The data to be posted</param>
        /// <param name="reqAccessToken">If an access token should be sent</param>
        /// <returns>Server response with returned data. If return is null, user should be logged out</returns>
        public static async Task<ServerResponse<T>> Put<T>(string route, object data, bool reqAccessToken = true)
        {
            try
            {
                using (var client = new HttpClient())
                {

                    if (reqAccessToken)
                    {
                        string accessToken = await GetAccessToken();

                        // Access and refresh tokens invalid, prompt new sign-in
                        if (accessToken == null) return null;

                        if (accessToken == ServerErr)
                        {
                            throw new Exception("There was an issue contacting the server");
                        }

                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }

                    string jsonContent = JsonConvert.SerializeObject(data, new JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 5,
                        Formatting = Formatting.None
                    });

                    HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    HttpResponseMessage resp = await client.PutAsync(ConfidentialData.api + route, content);
                    string json = await resp.Content.ReadAsStringAsync();

                    ServerResponse<T> toRet = new ServerResponse<T>
                    {
                        Success = resp.IsSuccessStatusCode,
                        Message = resp.ReasonPhrase,
                        StatusCode = resp.StatusCode,
                    };

                    if (toRet.Success && typeof(T) != typeof(string))
                    {
                        toRet.Data = JsonConvert.DeserializeObject<T>(json);
                    }

                    return toRet;
                }
            }
            catch (Exception e)
            {
                return new ServerResponse<T>
                {
                    Success = false,
                    Message = e.Message,
                    StatusCode = HttpStatusCode.BadRequest
                };
            }
        }

        /// <summary>
        /// Uploads a stream to the given route
        /// </summary>
        /// <typeparam name="T">Data type expected to be returned within the response</typeparam>
        /// <param name="route">The controller route (with first slash)</param>
        /// <param name="filename">What the file should be requested to be called</param>
        /// <param name="data">The file datastream</param>
        /// <returns>Server response with returned data. If return is null, user should be logged out</returns>
        public static async Task<ServerResponse<T>> UploadFile<T>(string route, string filename, Stream data)
        {
            try
            {
                if (uploadClient == null)
                {
                    uploadClient = new HttpClient
                    {
                        Timeout = TimeSpan.FromMinutes(30d)
                    };
                }

                string accessToken = await GetAccessToken();

                // Access and refresh tokens invalid, prompt new sign-in
                if (accessToken == null) return null;

                if (accessToken == ServerErr)
                {
                    throw new Exception("There was an issue contacting the server");
                }

                uploadClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new StreamContent(data);
                    fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = filename
                    };
                    content.Add(fileContent);

                    HttpResponseMessage resp = await uploadClient.PostAsync(ConfidentialData.api + "api/upload", content);
                    string json = await resp.Content.ReadAsStringAsync();

                    ServerResponse<T> toRet = new ServerResponse<T>
                    {
                        Success = resp.IsSuccessStatusCode,
                        Message = resp.ReasonPhrase
                    };

                    if (toRet.Success)
                    {
                        toRet.Data = JsonConvert.DeserializeObject<T>(json);
                    }

                    return toRet;
                }
            }
            catch (Exception e)
            {
                return new ServerResponse<T>
                {
                    Success = false,
                    Message = e.Message
                };
            }
        }

        public static async Task<ServerResponse<string>> UpdateAndPostResults(AppTask[] results, List<FileUpload> files, string uploadRoute)
        {
            // Update results which have had files uploaded to point at the new file URLs
            foreach (AppTask t in results)
            {
                if (t.TaskType.ReqFileUpload)
                {
                    string[] locals = JsonConvert.DeserializeObject<string[]>(t.CompletionData.JsonData) ?? new string[0];
                    for (int i = 0; i < locals.Length; i++)
                    {
                        FileUpload taskF = files.Where(fi => fi.LocalFilePath == locals[i]).FirstOrDefault();
                        if (taskF != null && !string.IsNullOrWhiteSpace(taskF.RemoteFilePath))
                        {
                            locals[i] = taskF.RemoteFilePath;
                            t.JsonData = taskF.RemoteFilePath;
                        }
                    }

                    t.CompletionData.JsonData = JsonConvert.SerializeObject(locals);
                }
            }
            return await Post<string>(uploadRoute, results);
        }

        public static Task<ServerResponse<string>> UploadActivity(AppDataUpload upload, bool updateExisting = false)
        {
            LearningActivity activity = JsonConvert.DeserializeObject<LearningActivity>(upload.JsonData);
            List<FileUpload> files = JsonConvert.DeserializeObject<List<FileUpload>>(upload.FilesJson);

            // update the activity's image url if it's had one uploaded
            FileUpload f = files?.Where(fi => fi.LocalFilePath == activity.ImageUrl).FirstOrDefault();
            if (f != null && !string.IsNullOrWhiteSpace(f.RemoteFilePath))
            {
                activity.ImageUrl = f.RemoteFilePath;
            }

            // Update tasks (and their children) which have had files uploaded
            // to point at the new file URLs
            foreach (LearningTask parentTask in activity.LearningTasks)
            {
                string newJson = GetNewTaskJsonData(parentTask, files);
                if (!string.IsNullOrWhiteSpace(newJson))
                {
                    parentTask.JsonData = newJson;
                }

                if (parentTask.ChildTasks == null) continue;

                foreach (LearningTask childTask in parentTask.ChildTasks)
                {
                    string newChildJson = GetNewTaskJsonData(childTask, files);
                    if (!string.IsNullOrWhiteSpace(newChildJson))
                    {
                        childTask.JsonData = newChildJson;
                    }
                }
            }

            return updateExisting ? Put<string>(upload.UploadRoute, activity) :
                Post<string>(upload.UploadRoute, activity);
        }

        private static string GetNewTaskJsonData(LearningTask task, List<FileUpload> files)
        {
            if (task.TaskType.IdName == "MATCH_PHOTO" || task.TaskType.IdName == "LISTEN_AUDIO" ||
                (task.TaskType.IdName == "DRAW_PHOTO" && !task.JsonData.StartsWith("TASK::", StringComparison.InvariantCulture)))
            {
                FileUpload taskF = files.FirstOrDefault(fi => fi.LocalFilePath == task.JsonData);
                if (taskF != null && !string.IsNullOrWhiteSpace(taskF.RemoteFilePath))
                {
                    return taskF.RemoteFilePath;
                }
                return null;
            }
            // Info is trickier, as the JsonData is an actual object, with the URL stored inside
            if (task.TaskType.IdName == "INFO")
            {
                AdditionalInfoData data = JsonConvert.DeserializeObject<AdditionalInfoData>(task.JsonData);
                FileUpload taskF = files.FirstOrDefault(fi => fi.LocalFilePath == data.ImageUrl);
                if (taskF != null)
                {
                    data.ImageUrl = taskF.RemoteFilePath;
                }
                return JsonConvert.SerializeObject(data);
            }

            return null;
        }
    }
}
