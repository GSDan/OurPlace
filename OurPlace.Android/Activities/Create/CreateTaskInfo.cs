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
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Transformations;
using FFImageLoading.Views;
using Newtonsoft.Json;
using OurPlace.Common.Models;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "New Info", Theme = "@style/OurPlaceActionBar")]
    public class CreateTaskInfo : AppCompatActivity
    {
        private EditText infoField;
        private EditText urlField;
        private TaskType taskType;
        private Button addTaskBtn;
        private ImageViewAsync imageView;
        global::Android.Net.Uri selectedImage;
        global::Android.Net.Uri outputFileUri;
        global::Android.Net.Uri previousFileUri;
        string finalImagePath;
        private int photoRequestCode = 111;
        private int permRequestCode = 222;

        private LearningTask newTask;
        private bool editing = false;
        private string editCachePath;
        private string originalPath;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CreateTaskInfo);

            infoField = FindViewById<EditText>(Resource.Id.taskInstructions);
            urlField = FindViewById<EditText>(Resource.Id.urlInput);
            imageView = FindViewById<ImageViewAsync>(Resource.Id.taskIcon);
            imageView.Click += ImageView_Click;
            addTaskBtn = FindViewById<Button>(Resource.Id.addTaskBtn);
            addTaskBtn.Click += AddTaskBtn_Click;

            Prepare();
        }

        private async void Prepare()
        {
            // Check if this is editing an existing task: if so, populate fields
            string editJson = Intent.GetStringExtra("EDIT") ?? "";
            newTask = JsonConvert.DeserializeObject<LearningTask>(editJson);

            if (newTask != null)
            {
                addTaskBtn.SetText(Resource.String.saveChanges);
                AdditionalInfoData addData = JsonConvert.DeserializeObject<AdditionalInfoData>(newTask.JsonData);
                infoField.Text = newTask.Description;
                urlField.Text = addData.ExternalUrl;
                taskType = newTask.TaskType;
                editing = true;

                if (!string.IsNullOrWhiteSpace(addData.ImageUrl) && File.Exists(addData.ImageUrl))
                {
                    editCachePath = Path.Combine(
                        Common.LocalData.Storage.GetCacheFolder(null),
                        "editcache-" + DateTime.UtcNow.ToString("MM-dd-yyyy-HH-mm-ss-fff"));
                    File.Copy(addData.ImageUrl, editCachePath, true);
                    Java.IO.File cachedFile = new Java.IO.File(editCachePath);
                    selectedImage = global::Android.Net.Uri.FromFile(cachedFile);
                    ImageService.Instance.LoadFile(selectedImage.Path).Transform(new CircleTransformation()).Into(imageView);
                    originalPath = addData.ImageUrl;
                }
            }
            else
            {
                // If edit is null, get the tasktype from JSON
                string jsonData = Intent.GetStringExtra("JSON") ?? "";
                taskType = JsonConvert.DeserializeObject<TaskType>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
                newTask = new LearningTask();
                newTask.TaskType = taskType;
            }

            if (selectedImage == null)
            {
                await ImageService.Instance.LoadUrl(taskType.IconUrl).IntoAsync(imageView);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (!string.IsNullOrWhiteSpace(editCachePath) && File.Exists(editCachePath))
            {
                File.Delete(editCachePath);
            }
        }

        private void UpdateFiles()
        {
            if (outputFileUri != null)
            {
                previousFileUri = outputFileUri;
            }

            string picturesDir = global::Android.OS.Environment.GetExternalStoragePublicDirectory(global::Android.OS.Environment.DirectoryPictures).AbsolutePath;
            string filePath = Path.Combine(picturesDir, string.Format("OurPlaceActivity-{0:yyyy-MM-dd_hh-mm-ss-tt}.jpg", DateTime.UtcNow));

            Java.IO.File newFile = new Java.IO.File(filePath);
            outputFileUri = global::Android.Net.Uri.FromFile(newFile);

            finalImagePath = Path.Combine(
                Common.LocalData.Storage.GetCacheFolder("created"),
                DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss-fff", CultureInfo.InvariantCulture) + ".jpg");
        }

        private void ImageView_Click(object sender, EventArgs e)
        {
            UpdateFiles();
            string permission = global::Android.Manifest.Permission.Camera;
            Permission currentPerm = ContextCompat.CheckSelfPermission(this, permission);
            if (currentPerm != Permission.Granted)
            {
                AndroidUtils.CheckGetPermission(permission,
                this, permRequestCode, Resources.GetString(Resource.String.permissionCameraTitle),
                Resources.GetString(Resource.String.permissionPhotoExplanation));
            }
            else
            {
                FirePhotoIntent(currentPerm == Permission.Granted);
            }
        }

        private void FirePhotoIntent(bool includeCamera)
        {
            Intent finalIntent = AndroidUtils.CreateMultiSourceImagePickerIntent(includeCamera, outputFileUri, this);
            StartActivityForResult(finalIntent, photoRequestCode);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == permRequestCode)
            {
                FirePhotoIntent(grantResults[0] == Permission.Granted);
            }
        }

        protected override async void OnActivityResult(int requestCode, [GeneratedEnum] global::Android.App.Result resultCode, Intent data)
        {
            bool success = resultCode == global::Android.App.Result.Ok;

            if (requestCode == photoRequestCode && success)
            {
                if (previousFileUri != null)
                {
                    try
                    {
                        await ImageService.Instance.LoadFile(previousFileUri.Path).InvalidateAsync(FFImageLoading.Cache.CacheType.All);

                        if (File.Exists(previousFileUri.Path))
                        {
                            File.Delete(previousFileUri.Path);
                        }
                        previousFileUri = null;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("WHY: " + e.Message);
                    }
                }

                selectedImage = await AndroidUtils.OnImagePickerResult(resultCode, data, outputFileUri, this, finalImagePath, 1920, 1080);

                if (selectedImage != null)
                {
                    ImageService.Instance.LoadFile(selectedImage.Path).Transform(new CircleTransformation()).Into(imageView);
                }
            }
        }

        private void AddTaskBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(infoField.Text))
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.ErrorTitle)
                    .SetMessage(Resource.String.createNewActivityTaskInstruct)
                    .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                    .Show();
                return;
            }

            AdditionalInfoData data = new AdditionalInfoData();

            if (!string.IsNullOrWhiteSpace(urlField.Text))
            {
                Uri uriResult;
                bool validUrl = Uri.TryCreate(urlField.Text, UriKind.Absolute, out uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (!validUrl)
                {
                    new global::Android.Support.V7.App.AlertDialog.Builder(this)
                        .SetTitle(Resource.String.ErrorTitle)
                        .SetMessage(Resource.String.createNewInfoUrlInvalid)
                        .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                        .Show();
                    return;
                }
                data.ExternalUrl = uriResult.AbsoluteUri;
            }

            if (selectedImage == null)
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.WarningTitle)
                    .SetMessage(Resource.String.createNewInfoNoImage)
                    .SetCancelable(false)
                    .SetNegativeButton(Resource.String.dialog_cancel, (a, b) => { return; })
                    .SetPositiveButton(Resource.String.Continue, (a, b) => { ContinueToNext(data); })
                    .Show();
                return;
            }

            if (!editing || selectedImage.Path != editCachePath)
            {
                data.ImageUrl = selectedImage.Path;
            }
            else
            {
                data.ImageUrl = originalPath;
            }

            ContinueToNext(data);
        }

        private void ContinueToNext(AdditionalInfoData data)
        {
            if (newTask == null)
            {
                newTask = new LearningTask();
            }

            newTask.Description = infoField.Text;
            newTask.TaskType = taskType;
            newTask.JsonData = JsonConvert.SerializeObject(data);

            string json = JsonConvert.SerializeObject(newTask, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                MaxDepth = 5
            });

            Intent myIntent = (editing) ?
                new Intent(this, typeof(CreateManageTasksActivity)) :
                new Intent(this, typeof(CreateChooseTaskTypeActivity));

            myIntent.PutExtra("JSON", json);
            SetResult(global::Android.App.Result.Ok, myIntent);
            Finish();
        }
    }
}