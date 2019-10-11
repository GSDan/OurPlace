using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Transformations;
using Newtonsoft.Json;
using OurPlace.Common;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Create a New Collection", Theme = "@style/OurPlaceActionBar", ParentActivity = typeof(MainActivity))]
    public class CreateCollectionActivity : AppCompatActivity
    {
        private ActivityCollection newCollection;
        private EditText titleInput;
        private EditText descInput;
        private Button continueButton;
        private ImageView imageView;

        private global::Android.Net.Uri selectedImage;
        private global::Android.Net.Uri outputFileUri;
        private global::Android.Net.Uri previousFileUri;
        private string finalImagePath;
        private const int PhotoRequestCode = 111;
        private const int PermRequestCode = 222;
        private Intent lastReqIntent;
        private bool editing;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.CreateNewActivity);

            titleInput = FindViewById<EditText>(Resource.Id.titleInput);
            descInput = FindViewById<EditText>(Resource.Id.descInput);
            imageView = FindViewById<ImageView>(Resource.Id.activityIcon);
            continueButton = FindViewById<Button>(Resource.Id.continueBtn);

            imageView.Click += ImageView_Click;
            continueButton.Click += ContinueButton_Click;

            FindViewById<TextInputLayout>(Resource.Id.collName_text_input_layout).Hint = GetString(Resource.String.createCollectionNameHint);
            FindViewById<TextInputLayout>(Resource.Id.collDesc_text_input_layout).Hint = GetString(Resource.String.createCollectionDescriptionHint);

            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            newCollection = JsonConvert.DeserializeObject<ActivityCollection>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            // load given details in if available
            if (newCollection != null)
            {
                editing = true;
                titleInput.Text = newCollection.Name;
                descInput.Text = newCollection.Description;

                if (!string.IsNullOrWhiteSpace(newCollection.ImageUrl))
                {
                    if (newCollection.ImageUrl.StartsWith("upload"))
                    {
                        selectedImage = global::Android.Net.Uri.Parse(newCollection.ImageUrl);
                        ImageService.Instance.LoadUrl(ServerUtils.GetUploadUrl(selectedImage.ToString()))
                            .Transform(new CircleTransformation())
                            .Into(imageView);
                    }
                    else
                    {
                        selectedImage = global::Android.Net.Uri.FromFile(new Java.IO.File(newCollection.ImageUrl));
                        ImageService.Instance.LoadFile(selectedImage.Path).Transform(new CircleTransformation()).Into(imageView);
                    }
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
            bool success = resultCode == Result.Ok;

            if (requestCode == PhotoRequestCode && success)
            {
                if (previousFileUri != null && !previousFileUri.ToString().StartsWith("upload"))
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
                    .SetMessage(Resource.String.createCollectionNameErr)
                    .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                    .Show();
                return;
            }

            if (string.IsNullOrWhiteSpace(descInput.Text))
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.ErrorTitle)
                    .SetMessage(Resource.String.createCollectionDescriptionErr)
                    .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                    .Show();
                return;
            }

            if (selectedImage == null)
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.WarningTitle)
                    .SetMessage(Resource.String.createCollectionNoImageWarning)
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
            if (newCollection == null)
            {
                ApplicationUser currentUser = (await Common.LocalData.Storage.GetDatabaseManager()).CurrentUser;

                newCollection = new ActivityCollection
                {
                    Author = new LimitedApplicationUser()
                    {
                        Id = currentUser.Id,
                        FirstName = currentUser.FirstName,
                        Surname = currentUser.Surname
                    },
                    Id = new Random().Next(),// Temp ID, used locally only
                    Activities = new List<LearningActivity>()
                };
            }

            newCollection.Name = titleInput.Text;
            newCollection.Description = descInput.Text;

            if (selectedImage != null)
            {
                newCollection.ImageUrl = selectedImage.Path;
            }

            Intent addActivitiesActivity = new Intent(this, typeof(CreateCollectionOverviewActivity));
            string json = JsonConvert.SerializeObject(newCollection, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                MaxDepth = 5
            });
            addActivitiesActivity.PutExtra("JSON", json);

            if (editing)
            {
                SetResult(Result.Ok, addActivitiesActivity);
            }
            else
            {
                StartActivity(addActivitiesActivity);
            }

            Finish();
        }
    }
}