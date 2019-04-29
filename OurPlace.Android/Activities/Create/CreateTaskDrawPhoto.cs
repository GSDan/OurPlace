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
using OurPlace.Common;
using OurPlace.Common.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "New Task", Theme = "@style/OurPlaceActionBar")]
    public class CreateTaskDrawPhoto : AppCompatActivity
    {
        private EditText instructions;
        private TaskType taskType;
        private ImageViewAsync chosenImageView;
        private global::Android.Net.Uri selectedImage;
        private global::Android.Net.Uri outputFileUri;
        private global::Android.Net.Uri previousFileUri;
        private string finalImagePath;
        private int photoRequestCode = 111;
        private int permRequestCode = 222;
        private LearningTask parentTask;
        private bool useParent;
        private LinearLayout chosenLayout;
        private TextView chosenTaskTextview;
        private Button addTaskBtn;
        private ImageViewAsync image;

        private LearningTask newTask;
        private bool editing = false;
        private bool isChild;
        private string editCachePath;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CreateTaskDrawPhoto);
            image = FindViewById<ImageViewAsync>(Resource.Id.taskIcon);
            instructions = FindViewById<EditText>(Resource.Id.taskInstructions);
            chosenImageView = FindViewById<ImageViewAsync>(Resource.Id.drawPhotoChosenImage);
            chosenLayout = FindViewById<LinearLayout>(Resource.Id.drawPhotoChosenLayout);
            chosenLayout.Visibility = global::Android.Views.ViewStates.Gone;
            FindViewById<Button>(Resource.Id.imageSourceChooseBtn).Click += ImageSourceChoose_Click;
            chosenTaskTextview = FindViewById<TextView>(Resource.Id.parentTaskNameText);
            chosenTaskTextview.Visibility = global::Android.Views.ViewStates.Gone;
            addTaskBtn = FindViewById<Button>(Resource.Id.addTaskBtn);
            addTaskBtn.Click += AddTaskBtn_Click;

            Prepare();
        }

        private void Prepare()
        {
            LearningTask passedTask = JsonConvert.DeserializeObject<LearningTask>(Intent.GetStringExtra("PARENT") ?? "");

            if (passedTask?.TaskType != null)
            {
                isChild = true;
                string[] supportedParents = { "TAKE_PHOTO", "MATCH_PHOTO", "DRAW", "DRAW_PHOTO" };

                if (supportedParents.Contains(passedTask.TaskType.IdName))
                {
                    // We can use the parent task as an image source
                    parentTask = passedTask;

                    Button useParentBtn = FindViewById<Button>(Resource.Id.useParentBtn);
                    useParentBtn.Visibility = global::Android.Views.ViewStates.Visible;
                    useParentBtn.Click += UseParentBtn_Click;
                    useParentBtn.Text = string.Format(GetString(
                        Resource.String.createNewDrawPhotoChooseParent), passedTask.Description);
                }
            }

            // Check if this is editing an existing task: if so, populate fields
            string editJson = Intent.GetStringExtra("EDIT") ?? "";
            newTask = JsonConvert.DeserializeObject<LearningTask>(editJson);

            if (newTask != null)
            {
                addTaskBtn.SetText(Resource.String.saveChanges);
                LoadExisting();
            }
            else
            {
                string jsonData = Intent.GetStringExtra("JSON") ?? "";
                taskType = JsonConvert.DeserializeObject<TaskType>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            }

            FindViewById<TextView>(Resource.Id.taskTypeNameText).Text = taskType.DisplayName;
            AndroidUtils.LoadTaskTypeIcon(taskType, image);
        }

        private void LoadExisting()
        {
            instructions.Text = newTask.Description;
            taskType = newTask.TaskType;
            editing = true;

            if (newTask.JsonData.StartsWith("TASK::", StringComparison.InvariantCulture))
            {
                UseParentBtn_Click(null, null);
            }
            else
            {
                if (!newTask.JsonData.StartsWith("upload"))
                {
                    // Copy the existing file, in case the user overwrites it but then doesn't want to save changes
                    editCachePath = Path.Combine(
                        Common.LocalData.Storage.GetCacheFolder(),
                        "editcache-" + DateTime.UtcNow.ToString("MM-dd-yyyy-HH-mm-ss-fff"));

                    File.Copy(newTask.JsonData, editCachePath, true);

                    Java.IO.File cachedFile = new Java.IO.File(editCachePath);
                    selectedImage = global::Android.Net.Uri.FromFile(cachedFile);
                }

                ShowImage();
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

        private async Task UpdateFiles()
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
                DateTime.UtcNow.ToString("MM-dd-yyyy-HH-mm-ss-fff", CultureInfo.InvariantCulture) + ".jpg");
        }

        private async void ImageSourceChoose_Click(object sender, EventArgs e)
        {
            await UpdateFiles();
            TakeChoosePhoto();
            return;
        }

        private void UseParentBtn_Click(object sender, EventArgs e)
        {
            useParent = true;
            chosenLayout.Visibility = global::Android.Views.ViewStates.Gone;
            chosenTaskTextview.Visibility = global::Android.Views.ViewStates.Visible;
            selectedImage = null;
            chosenTaskTextview.Text = string.Format(Resources.GetString(
                Resource.String.createNewDrawPhotoChosenTask), parentTask.Description);
        }

        /// <summary>
        /// Launch image source picker intent, after trying to get permission for camera
        /// </summary>
        private void TakeChoosePhoto()
        {
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

        private void ShowImage()
        {
            if (selectedImage != null)
            {
                ImageService.Instance.LoadFile(selectedImage.Path).Transform(new CircleTransformation()).Into(chosenImageView);
                chosenLayout.Visibility = global::Android.Views.ViewStates.Visible;
                chosenTaskTextview.Visibility = global::Android.Views.ViewStates.Gone;
                useParent = false;
            }
            else if (newTask != null && newTask.JsonData.StartsWith("upload"))
            {
                ImageService.Instance.LoadUrl(ServerUtils.GetUploadUrl(newTask.JsonData))
                    .Transform(new CircleTransformation())
                    .Into(chosenImageView);
                chosenLayout.Visibility = global::Android.Views.ViewStates.Visible;
                chosenTaskTextview.Visibility = global::Android.Views.ViewStates.Gone;
                useParent = false;
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

            if (selectedImage == null &&
                useParent == false &&
                (newTask == null || string.IsNullOrWhiteSpace(newTask.JsonData)))
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.ErrorTitle)
                    .SetMessage(Resource.String.createNewDrawPhotoNothingSelected)
                    .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                    .Show();
                return;
            }

            if (newTask == null)
            {
                newTask = new LearningTask
                {
                    TaskType = taskType
                };
            }

            newTask.Description = instructions.Text;

            if (selectedImage != null)
            {
                if (!editing || selectedImage.Path != editCachePath)
                {
                    newTask.JsonData = selectedImage.Path;
                }
            }
            else if (useParent)
            {
                newTask.JsonData = "TASK::" + parentTask.Id;
                if (outputFileUri != null && File.Exists(outputFileUri.Path))
                {
                    File.Delete(outputFileUri.Path);
                }
            }

            string json = JsonConvert.SerializeObject(newTask, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                MaxDepth = 5
            });

            Intent myIntent;

            if (isChild && editing)
            {
                myIntent = new Intent(this, typeof(CreateManageChildTasksActivity));
            }
            else
            {
                myIntent = (editing) ?
                    new Intent(this, typeof(CreateActivityOverviewActivity)) :
                    new Intent(this, typeof(CreateChooseTaskTypeActivity));
            }

            myIntent.PutExtra("JSON", json);
            SetResult(global::Android.App.Result.Ok, myIntent);
            Finish();
        }
    }
}