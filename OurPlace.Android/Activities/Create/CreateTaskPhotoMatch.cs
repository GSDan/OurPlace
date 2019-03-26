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
using OurPlace.Common;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "New Task", Theme = "@style/OurPlaceActionBar")]
    public class CreateTaskPhotoMatch : AppCompatActivity
    {
        private EditText instructions;
        private TaskType taskType;
        private ImageViewAsync chosenImageView;
        private LinearLayout chosenLayout;
        private Button addTaskBtn;
        private ImageViewAsync image;
        private global::Android.Net.Uri selectedImage;
        private global::Android.Net.Uri outputFileUri;
        private global::Android.Net.Uri previousFileUri;
        private string finalImagePath;
        private int photoRequestCode = 111;
        private int permRequestCode = 222;

        private LearningTask newTask;
        private bool editing = false;
        private string editCachePath;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CreateTaskPhotoMatch);

            FindViewById<Button>(Resource.Id.photoMatchChooseBtn).Click += CreateTaskPhotoMatch_Click;
            image = FindViewById<ImageViewAsync>(Resource.Id.taskIcon);
            addTaskBtn = FindViewById<Button>(Resource.Id.addTaskBtn);
            addTaskBtn.Click += AddTaskBtn_Click;
            instructions = FindViewById<EditText>(Resource.Id.taskInstructions);

            chosenImageView = FindViewById<ImageViewAsync>(Resource.Id.photoMatchChosenImage);
            chosenLayout = FindViewById<LinearLayout>(Resource.Id.photoMatchChosenLayout);
            chosenLayout.Visibility = global::Android.Views.ViewStates.Gone;

            Prepare();
        }

        private void Prepare()
        {
            // Check if this is editing an existing task: if so, populate fields
            string editJson = Intent.GetStringExtra("EDIT") ?? "";
            newTask = JsonConvert.DeserializeObject<LearningTask>(editJson);

            if (newTask != null)
            {
                editing = true;
                addTaskBtn.SetText(Resource.String.saveChanges);
                taskType = newTask.TaskType;
                instructions.Text = newTask.Description;
                editCachePath = Path.Combine(Common.LocalData.Storage.GetCacheFolder(null), "editcache-" + DateTime.UtcNow.ToString("MM-dd-yyyy-HH-mm-ss-fff"));

                if (newTask.JsonData.StartsWith("upload"))
                {
                    selectedImage = global::Android.Net.Uri.Parse(newTask.JsonData);
                }
                else
                {
                    File.Copy(newTask.JsonData, editCachePath, true);
                    Java.IO.File cachedFile = new Java.IO.File(editCachePath);
                    selectedImage = global::Android.Net.Uri.FromFile(cachedFile);
                }
                
                ShowImage();
            }
            else
            {
                string jsonData = Intent.GetStringExtra("JSON") ?? "";
                taskType = JsonConvert.DeserializeObject<TaskType>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            }

            FindViewById<TextView>(Resource.Id.taskTypeNameText).Text = taskType.DisplayName;
            AndroidUtils.LoadTaskTypeIcon(taskType, image);
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

            finalImagePath = Path.Combine(Common.LocalData.Storage.GetCacheFolder("created"), DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss-fff", CultureInfo.InvariantCulture) + ".jpg");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (!string.IsNullOrWhiteSpace(editCachePath) && File.Exists(editCachePath))
            {
                File.Delete(editCachePath);
            }
        }

        private void CreateTaskPhotoMatch_Click(object sender, EventArgs e)
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

                selectedImage = await AndroidUtils.OnImagePickerResult(resultCode, data, outputFileUri, this, finalImagePath, 1920, 1200);
                ShowImage();
            }
        }

        private void ShowImage()
        {
            if (selectedImage == null) return;

            if (selectedImage.ToString().StartsWith("upload"))
            {
                ImageService.Instance.LoadUrl(ServerUtils.GetUploadUrl(selectedImage.ToString()))
                    .Transform(new CircleTransformation()).Into(chosenImageView);
            }
            else
            {
                ImageService.Instance.LoadFile(selectedImage.Path)
                    .Transform(new CircleTransformation()).Into(chosenImageView);
            }
            
            chosenLayout.Visibility = global::Android.Views.ViewStates.Visible;
        }

        private void FirePhotoIntent(bool includeCamera)
        {
            Intent finalIntent = AndroidUtils.CreateMultiSourceImagePickerIntent(includeCamera, outputFileUri, this);
            StartActivityForResult(finalIntent, photoRequestCode);
        }

        private void AddTaskBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(instructions.Text))
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.ErrorTitle)
                    .SetMessage(Resource.String.createNewActivityTaskInstruct)
                    .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                    .Show();
                return;
            }

            if (selectedImage == null)
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.ErrorTitle)
                    .SetMessage(Resource.String.createNewPhotoMatchErr)
                    .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                    .Show();
                return;
            }

            if (newTask == null)
            {
                newTask = new LearningTask() { TaskType = taskType };
            }

            newTask.Description = instructions.Text;
            if (!editing || selectedImage.Path != editCachePath)
            {
                newTask.JsonData = selectedImage.Path;
            }

            string json = JsonConvert.SerializeObject(newTask, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                MaxDepth = 5
            });

            Intent myIntent = new Intent(this, typeof(CreateChooseTaskTypeActivity));
            myIntent.PutExtra("JSON", json);
            SetResult(global::Android.App.Result.Ok, myIntent);
            Finish();
        }
    }
}