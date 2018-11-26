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
    [Activity(Label = "Create A New Activity", Theme = "@style/OurPlaceActionBar", ParentActivity = typeof(MainActivity))]
    public class CreateNewActivity : AppCompatActivity
    {
        private EditText titleInput;
        private EditText descInput;
        private Button continueButton;
        private ImageViewAsync imageView;
        private global::Android.Net.Uri selectedImage;
        private global::Android.Net.Uri outputFileUri;
        private global::Android.Net.Uri previousFileUri;
        private string finalImagePath;
        private const int PhotoRequestCode = 111;
        private const int PermRequestCode = 222;
        private Intent lastReqIntent;
        private bool editing;

        private LearningActivity newActivity;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.CreateNewActivity);

            titleInput = FindViewById<EditText>(Resource.Id.titleInput);
            descInput = FindViewById<EditText>(Resource.Id.descInput);
            imageView = FindViewById<ImageViewAsync>(Resource.Id.activityIcon);
            continueButton = FindViewById<Button>(Resource.Id.continueBtn);

            imageView.Click += ImageView_Click;
            continueButton.Click += ContinueButton_Click;

#if DEBUG
            titleInput.Text = "DEBUG TITLE";
            descInput.Text = "DEBUG DESCRIPTION";
#endif

            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            newActivity = JsonConvert.DeserializeObject<LearningActivity>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            // load given details in if available
            if (newActivity != null)
            {
                editing = true;
                titleInput.Text = newActivity.Name;
                descInput.Text = newActivity.Description;

                if (!string.IsNullOrWhiteSpace(newActivity.ImageUrl))
                {
                    selectedImage = global::Android.Net.Uri.FromFile(new Java.IO.File(newActivity.ImageUrl));
                    ImageService.Instance.LoadFile(selectedImage.Path).Transform(new CircleTransformation()).Into(imageView);
                }
            }
        }

        private void UpdateFiles()
        {
            if (outputFileUri != null && File.Exists(outputFileUri.Path))
            {
                previousFileUri = outputFileUri;
            }

            string picturesDir = global::Android.OS.Environment.GetExternalStoragePublicDirectory(global::Android.OS.Environment.DirectoryPictures).AbsolutePath;
            string filePath = Path.Combine(picturesDir, string.Format("OurPlaceActivity-{0:yyyy-MM-dd_hh-mm-ss-tt}.jpg", DateTime.Now));

            Java.IO.File newFile = new Java.IO.File(filePath);
            outputFileUri = global::Android.Net.Uri.FromFile(newFile);

            finalImagePath = Path.Combine(
                Common.LocalData.Storage.GetCacheFolder("created"),
                DateTime.Now.ToString(
                    "MM-dd-yyyy-HH-mm-ss-fff",
                    CultureInfo.InvariantCulture)
                + ".jpg");
        }

        private void ImageView_Click(object sender, EventArgs e)
        {
            UpdateFiles();
            lastReqIntent = AndroidUtils.CreateMultiSourceImagePickerIntent(true, outputFileUri, this);

            // Requires both camera and storage permissions
            AndroidUtils.CallWithPermission(new string[]{
                global::Android.Manifest.Permission.Camera,
                global::Android.Manifest.Permission.ReadExternalStorage,
                global::Android.Manifest.Permission.WriteExternalStorage
            }, new string[] {
                Resources.GetString(Resource.String.permissionCameraTitle),
                Resources.GetString(Resource.String.permissionFilesTitle),
                Resources.GetString(Resource.String.permissionFilesTitle)
            }, new string[] {
                Resources.GetString(Resource.String.permissionPhotoExplanation),
                Resources.GetString(Resource.String.permissionFilesExplanation),
                Resources.GetString(Resource.String.permissionFilesExplanation)
            }, lastReqIntent, PhotoRequestCode, PermRequestCode, this);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == PermRequestCode)
            {
                StartActivityForResult(lastReqIntent, PhotoRequestCode);
            }
        }

        protected override async void OnActivityResult(int requestCode, [GeneratedEnum] global::Android.App.Result resultCode, Intent data)
        {
            bool success = resultCode == global::Android.App.Result.Ok;

            if (requestCode == PhotoRequestCode && success)
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

        private void ContinueButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(titleInput.Text))
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.ErrorTitle)
                    .SetMessage(Resource.String.createNewActivityErrNoTitle)
                    .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                    .Show();
                return;
            }

            if (string.IsNullOrWhiteSpace(descInput.Text))
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.ErrorTitle)
                    .SetMessage(Resource.String.createNewActivityErrNoDesc)
                    .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                    .Show();
                return;
            }

            if (selectedImage == null)
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.WarningTitle)
                    .SetMessage(Resource.String.createNewActivityWarnNoImage)
                    .SetCancelable(false)
                    .SetNegativeButton(Resource.String.dialog_cancel, (a, b) => { return; })
                    .SetPositiveButton(Resource.String.Continue, (a, b) => { ContinueToNext(); })
                    .Show();
                return;
            }

            var suppress = ContinueToNext();
        }

        private async Task ContinueToNext()
        {
            if (newActivity == null)
            {
                newActivity = new LearningActivity
                {
                    Author = (await Common.LocalData.Storage.GetDatabaseManager()).currentUser,
                    Id = new Random().Next() // Temp ID, used locally only
                };
            }

            newActivity.Name = titleInput.Text;
            newActivity.Description = descInput.Text;

            if (selectedImage != null)
            {
                newActivity.ImageUrl = selectedImage.Path;
            }

            Intent addTasksActivity = new Intent(this, typeof(CreateActivityOverviewActivity));
            string json = JsonConvert.SerializeObject(newActivity, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                MaxDepth = 5
            });
            addTasksActivity.PutExtra("JSON", json);

            if (editing)
            {
                SetResult(Result.Ok, addTasksActivity);
            }
            else
            {
                StartActivity(addTasksActivity);
            }

            Finish();
        }
    }
}