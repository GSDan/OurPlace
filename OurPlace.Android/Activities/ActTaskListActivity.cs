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
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Android.Activities.Abstracts;
using OurPlace.Android.Activities.Create;
using OurPlace.Android.Adapters;
using OurPlace.Android.Fragments;
using OurPlace.Common;
using OurPlace.Common.LocalData;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OurPlace.Android.Activities
{
    [Activity(Label = "OurPlace", ParentActivity = typeof(MainActivity), LaunchMode = LaunchMode.SingleTop)]
    public class ActTaskListActivity : HeaderImageActivity
    {
        private global::Android.Support.V7.Widget.Toolbar toolbar;
        private LearningActivity learningActivity;
        private RecyclerView recyclerView;
        private RecyclerView.LayoutManager layoutManager;
        private TaskAdapter adapter;
        private DatabaseManager dbManager;
        private const int MediaPlayerReqCode = 222;
        private const int PermReqId = 111;
        private string enteredName = "";
        private Intent lastReqIntent;
        private bool shouldSave = true;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            learningActivity = JsonConvert.DeserializeObject<LearningActivity>(jsonData,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            if (learningActivity == null)
            {
                global::Android.Support.V7.App.AlertDialog.Builder alert = new global::Android.Support.V7.App.AlertDialog.Builder(this);
                alert.SetTitle(Resource.String.ErrorTitle);
                alert.SetMessage(Resource.String.ErrorTitle);
                alert.SetOnDismissListener(new OnDismissListener(Finish));
                alert.Show();
                return;
            }

            dbManager = await Storage.GetDatabaseManager();

            // Load this activity's progress from the database if available
            ActivityProgress progress = dbManager.GetProgress(learningActivity);
            List<AppTask> appTasks = null;

            try
            {
                if (progress != null)
                {
                    enteredName = progress.EnteredUsername;
                    appTasks = JsonConvert.DeserializeObject<List<AppTask>>(progress.AppTaskJson);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Toast.MakeText(this, Resource.String.errorCache, ToastLength.Long).Show();
                appTasks = null;
            }

            if (appTasks == null)
            {
                appTasks = learningActivity.LearningTasks.Select(t => new AppTask(t)).ToList();
            }

            bool curatorControls = learningActivity.IsPublic && !learningActivity.Approved && dbManager.currentUser.Trusted;

            adapter = new TaskAdapter(this, appTasks, learningActivity.Description, curatorControls, learningActivity.RequireUsername);
            adapter.ItemClick += OnItemClick;
            adapter.TextEntered += Adapter_TextEntered;
            adapter.ShowMedia += ShowMedia;
            adapter.Approved += Adapter_Approved;
            adapter.SpeakText += Adapter_SpeakText;
            adapter.ChangeName += Adapter_EditName;

            SetContentView(Resource.Layout.RecyclerViewActivity);
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            toolbar = FindViewById<global::Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            collapsingToolbar = FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            LoadHeaderImage(learningActivity.ImageUrl);

            SetupContent();

            if (!string.IsNullOrWhiteSpace(enteredName))
            {
                adapter.UpdateNames(enteredName);
            }
        }

        private void ShowNameEntry(bool continueToFinish = false)
        {
            global::Android.Support.V7.App.AlertDialog.Builder builder = new global::Android.Support.V7.App.AlertDialog.Builder(this);
            builder.SetTitle(Resource.String.actEnterUsernameTitle);

            int px = (int)global::Android.Util.TypedValue.ApplyDimension(
                global::Android.Util.ComplexUnitType.Dip, 16, base.Resources.DisplayMetrics);

            TextView message = new TextView(this);
            message.SetText(Resource.String.actEnterUsername);
            EditText nameInput = new EditText(this) {Text = enteredName};

            LinearLayout dialogLayout = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical
            };

            dialogLayout.AddView(message);
            dialogLayout.AddView(nameInput);
            dialogLayout.SetPadding(px, px, px, px);

            builder.SetView(dialogLayout);
            builder.SetNeutralButton(Resource.String.dialog_cancel, (a, b) => { });
            builder.SetPositiveButton(Resource.String.dialog_ok, (a, b) =>
            {
                if (string.IsNullOrWhiteSpace(nameInput.Text))
                {
                    // If nothing has been entered and nothing has been previously
                    // entered, show the dialog again
                    if (string.IsNullOrWhiteSpace(enteredName))
                    {
                        ShowNameEntry();
                    }
                }
                else
                {
                    enteredName = nameInput.Text;
                    adapter.UpdateNames(enteredName);
                    dbManager.SaveActivityProgress(learningActivity, adapter.Items, enteredName);

                    if(continueToFinish)
                    {
                        PackageForUpload();
                    }
                }
            });

            if (!string.IsNullOrWhiteSpace(enteredName))
            {
                builder.SetNeutralButton(Resource.String.dialog_cancel, (a, b) => { });
            }

            builder.Show();
        }

        private async void SetupContent()
        {
            try
            {
                bool success = await AndroidUtils.PrepActivityFiles(this, learningActivity);
                if (!success)
                {
                    Toast.MakeText(this, $"{GetString(Resource.String.ConnectionError)}", ToastLength.Long).Show();
                    Finish();
                    return;
                }
            }
            catch (Exception e)
            {
                Toast.MakeText(this, $"{GetString(Resource.String.ErrorTitle)}: {e.Message}", ToastLength.Long).Show();
                Finish();
                return;
            }

            recyclerView.SetAdapter(adapter);
            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);

            ChildItemDecoration childDecoration = new ChildItemDecoration(this, 20);
            recyclerView.AddItemDecoration(childDecoration);
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (learningActivity == null || adapter?.Items == null)
            {
                Finish();
            }
        }

        private void Adapter_SpeakText(object sender, int position)
        {
            if (adapter.ItemCount <= position)
            {
                return;
            }

            string toRead = (position > 0) ?
                adapter.Items[position].Description : learningActivity.Description;

            tts.ReadText(toRead);
        }

        private void Adapter_EditName(object sender, int position)
        {
            ShowNameEntry();
        }

        private void Adapter_Approved(object sender, bool activityApproved)
        {
            if (activityApproved)
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.approveActivityBtnConfirmTitle)
                    .SetMessage(Resource.String.approveActivityBtnApproveConfirm)
                    .SetNegativeButton(Resource.String.dialog_cancel, (a, b) => { })
                    .SetPositiveButton(Resource.String.approveActivityBtnApprove, (a, b) =>
                    {
                        ApproveActivity();
                    })
                    .Show();
            }
            else
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.approveActivityBtnConfirmTitle)
                    .SetMessage(Resource.String.approveActivityBtnDeleteConfirm)
                    .SetNegativeButton(Resource.String.dialog_cancel, (a, b) => { })
                    .SetPositiveButton(Resource.String.approveActivityBtnDelete, (a, b) =>
                    {
                        DeleteActivity();
                    })
                    .Show();
            }
        }

        private async void ApproveActivity()
        {
            ProgressDialog progress = new ProgressDialog(this);
            progress.SetMessage(Resources.GetString(Resource.String.PleaseWait));
            progress.Show();
            ServerResponse<string> resp = await ServerUtils.Post<string>("/api/learningactivities/approve?id=" + learningActivity.Id, null);
            progress.Dismiss();

            if (resp == null)
            {
                var suppress = AndroidUtils.ReturnToSignIn(this);
                Toast.MakeText(this, Resource.String.ForceSignOut, ToastLength.Long).Show();
                return;
            }

            if (resp.Success)
            {
                Toast.MakeText(this, Resource.String.uploadsUploadSuccessTitle, ToastLength.Long).Show();
            }
            else
            {
                Toast.MakeText(this, Resource.String.ConnectionError, ToastLength.Long).Show();
                return;
            }

            MainLandingFragment.ForceRefresh = true;

            adapter.Items.RemoveAt(0);
            adapter.Curator = false;
            learningActivity.Approved = true;

            dbManager.SaveActivityProgress(learningActivity, adapter.Items, enteredName);

            adapter.NotifyDataSetChanged();
        }

        private async void DeleteActivity()
        {
            ProgressDialog progress = new ProgressDialog(this);
            progress.SetMessage(Resources.GetString(Resource.String.PleaseWait));
            progress.Show();
            ServerResponse<string> resp = await ServerUtils.Delete<string>("/api/learningactivities?id=" + learningActivity.Id);
            progress.Dismiss();

            if (resp == null)
            {
                var suppress = AndroidUtils.ReturnToSignIn(this);
                Toast.MakeText(this, Resource.String.ForceSignOut, ToastLength.Long).Show();
                return;
            }

            if (resp.Success)
            {
                Toast.MakeText(this, Resource.String.uploadsUploadSuccessTitle, ToastLength.Long).Show();
            }
            else
            {
                Toast.MakeText(this, Resource.String.ConnectionError, ToastLength.Long).Show();
            }
            MainLandingFragment.ForceRefresh = true;

            dbManager.DeleteProgress(learningActivity.Id);

            Finish();
        }

        private void Adapter_TextEntered(object sender, int position)
        {
            if (adapter.Items[position].TaskType.IdName != "ENTER_TEXT")
            {
                return;
            }

            string entered = ((EditText)sender).Text;
            adapter.Items[position].CompletionData.JsonData = entered;
            adapter.Items[position].IsCompleted = !string.IsNullOrWhiteSpace(entered);

            if (!adapter.OnBind)
            {
                adapter.NotifyItemChanged(adapter.ItemCount - 1);
            }
        }

        private void ShowMedia(int taskIndex, int pathIndex)
        {
            Intent viewActivity = new Intent(this, typeof(MediaViewerActivity));
            viewActivity.PutExtra("RES_INDEX", pathIndex);
            viewActivity.PutExtra("ACT_ID", learningActivity.Id);
            viewActivity.PutExtra("JSON", JsonConvert.SerializeObject(adapter.Items[taskIndex]));

            adapter.Items[taskIndex].IsCompleted = true;
            adapter.CheckForChildren(taskIndex);

            StartActivityForResult(viewActivity, MediaPlayerReqCode);
        }

        // Called when the user has given/denied permission
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode != PermReqId)
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
                return;
            }

            bool hasPerm = grantResults.All(t => t == Permission.Granted);

            if (hasPerm)
            {
                StartActivityForResult(lastReqIntent, 111);
            }
            else
            {
                Toast.MakeText(this, Resources.GetString(Resource.String.permissionRefused), ToastLength.Long).Show();
            }
        }

        private void OnItemClick(object sender, int position)
        {
            // If Finish button clicked
            if (position == adapter.ItemCount - 1)
            {
                PackageForUpload();
                return;
            }

            string json = JsonConvert.SerializeObject(adapter.Items[position],
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    MaxDepth = 5
                });

            string taskType = adapter.Items[position].TaskType.IdName;

            switch (taskType)
            {
                case "TAKE_VIDEO":
                case "TAKE_PHOTO":
                case "MATCH_PHOTO":
                {
                    lastReqIntent = new Intent(this, typeof(CameraActivity));
                    lastReqIntent.PutExtra("JSON", json);
                    lastReqIntent.PutExtra("ACTID", learningActivity.Id);

                    List<string> perms = new List<string>
                    {
                        global::Android.Manifest.Permission.Camera,
                        global::Android.Manifest.Permission.AccessFineLocation
                    };
                    List<string> titles = new List<string>
                    {
                        base.Resources.GetString(Resource.String.permissionCameraTitle),
                        base.Resources.GetString(Resource.String.permissionLocationTitle)
                    };
                    List<string> explanations = new List<string>
                    {
                        base.Resources.GetString(Resource.String.permissionPhotoExplanation),
                        base.Resources.GetString(Resource.String.permissionLocationExplanation)
                    };

                    // Video tasks also require the microphone
                    if (taskType == "TAKE_VIDEO")
                    {
                        perms.Add(global::Android.Manifest.Permission.RecordAudio);
                        titles.Add(base.Resources.GetString(Resource.String.permissionMicTitle));
                        explanations.Add(base.Resources.GetString(Resource.String.permissionMicExplanation));
                    }

                    AndroidUtils.CallWithPermission(perms.ToArray(), titles.ToArray(), explanations.ToArray(),
                        lastReqIntent, adapter.Items[position].Id, PermReqId, this);
                    break;
                }

                case "DRAW":
                case "DRAW_PHOTO":
                {
                    Intent drawActivity = new Intent(this, typeof(DrawingActivity));
                    drawActivity.PutExtra("JSON", json);

                    if (taskType == "DRAW_PHOTO" && adapter.Items[position].JsonData.StartsWith("TASK::", StringComparison.OrdinalIgnoreCase))
                    {
                        int id = -1;
                        int.TryParse(adapter.Items[position].JsonData.Substring(6), out id);
                        string[] paths = JsonConvert.DeserializeObject<string[]>(adapter.GetTaskWithId(id).CompletionData.JsonData);
                        drawActivity.PutExtra("PREVIOUS_PHOTO", paths[0]);
                    }

                    StartActivityForResult(drawActivity, adapter.Items[position].Id);
                    break;
                }

                case "MAP_MARK":
                    lastReqIntent = new Intent(this, typeof(LocationMarkerActivity));
                    lastReqIntent.PutExtra("JSON", json);

                    AndroidUtils.CallWithPermission(new string[] { global::Android.Manifest.Permission.AccessFineLocation },
                        new string[] { base.Resources.GetString(Resource.String.permissionLocationTitle) },
                        new string[] { base.Resources.GetString(Resource.String.permissionLocationExplanation) },
                        lastReqIntent, adapter.Items[position].Id, PermReqId, this);
                    break;

                case "LOC_HUNT":
                    lastReqIntent = new Intent(this, typeof(LocationHuntActivity));
                    lastReqIntent.PutExtra("JSON", json);

                    AndroidUtils.CallWithPermission(new string[] { global::Android.Manifest.Permission.AccessFineLocation },
                        new string[] { base.Resources.GetString(Resource.String.permissionLocationTitle) },
                        new string[] { base.Resources.GetString(Resource.String.permissionLocationExplanation) },
                        lastReqIntent, adapter.Items[position].Id, PermReqId, this);
                    break;

                case "SCAN_QR":
                    lastReqIntent = new Intent(this, typeof(ScanningActivity));
                    lastReqIntent.PutExtra("JSON", json);
                    StartActivityForResult(lastReqIntent, adapter.Items[position].Id);
                    break;

                case "REC_AUDIO":
                    lastReqIntent = new Intent(this, typeof(RecordAudioActivity));
                    lastReqIntent.PutExtra("JSON", json);

                    AndroidUtils.CallWithPermission(new string[] { global::Android.Manifest.Permission.RecordAudio },
                        new string[] { base.Resources.GetString(Resource.String.permissionMicTitle) },
                        new string[] { base.Resources.GetString(Resource.String.permissionMicExplanation) },
                        lastReqIntent, adapter.Items[position].Id, PermReqId, this);
                    break;

                case "LISTEN_AUDIO":
                    ShowMedia(position, -1);
                    break;

                case "INFO":
                    AdditionalInfoData data = JsonConvert.DeserializeObject<AdditionalInfoData>(adapter.Items[position].JsonData);
                    global::Android.Net.Uri uri = global::Android.Net.Uri.Parse(data.ExternalUrl);
                    Intent intent = new Intent(Intent.ActionView, uri);
                    StartActivity(intent);
                    break;

                default:
                    Toast.MakeText(this, "Unknown task type! :(", ToastLength.Short).Show();
                    break;
            }
        }

        public override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            if (learningActivity != null)
            {
                toolbar.Title = learningActivity.Name;
            }
        }

        // Handle files and/or data being returned from launched task activities
        protected override void OnActivityResult(int requestCode, [GeneratedEnum] global::Android.App.Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (data == null || resultCode != Result.Ok)
            {
                return;
            }

            int taskId = data.GetIntExtra("TASK_ID", -1);
            int resIndex = data.GetIntExtra("RES_INDEX", -1);

            // deleting from MediaViewerActivity
            if (data.GetBooleanExtra("IS_DELETE", false) && taskId != -1 && resIndex != -1)
            {
                adapter.DeleteFile(taskId, resIndex);
                return;
            }

            string newFile = data.GetStringExtra("FILE_PATH");
            string mapLocations = data.GetStringExtra("LOCATIONS");
            bool isPoly = data.GetBooleanExtra("IS_POLYGON", false);
            bool complete = data.GetBooleanExtra("COMPLETE", false);

            if (resultCode == Result.Ok && taskId != -1 && !string.IsNullOrWhiteSpace(newFile))
            {
                string taskType = adapter.GetTaskWithId(taskId).TaskType.IdName;

                adapter.OnFileReturned(taskId, newFile,
                    (taskType == "TAKE_PHOTO" || taskType == "MATCH_PHOTO"));
            }
            else if (!string.IsNullOrWhiteSpace(mapLocations))
            {
                adapter.OnMapReturned(taskId, mapLocations, isPoly);
            }
            else if (complete)
            {
                // A task which doesn't return a file or data has been completed (e.g. location hunt)
                adapter.OnGenericTaskReturned(taskId, true);
            }
            else
            {
                Toast.MakeText(this, "Error: Unknown task type", ToastLength.Short).Show();
            }

            dbManager.SaveActivityProgress(learningActivity, adapter.Items, enteredName);
        }

        /// <summary>
        /// Package the entered/created data up for storage
        /// </summary>
        public void PackageForUpload()
        {
            if (learningActivity.RequireUsername && string.IsNullOrWhiteSpace(enteredName))
            {
                ShowNameEntry(true);
                return;
            }

            List<AppTask> preppedTasks = new List<AppTask>();
            foreach (AppTask t in adapter.Items)
            {
                if (t == null)
                {
                    continue;
                }

                preppedTasks.Add(Storage.PrepForUpload(t));
            }
         
            // Skip packaging the upload if there is no entered data
            bool anyData = false;
            foreach (AppTask t in preppedTasks)
            {
                if (!string.IsNullOrWhiteSpace(t.CompletionData.JsonData))
                {
                    anyData = true;
                }
            }

            if (!anyData)
            {
                Finish();
                return;
            }


            ApplicationUser creator = learningActivity.Author;
            if (creator != null && creator.Id != dbManager.currentUser.Id)
            {
                string name = creator.FirstName[0] + ". " + creator.Surname;

                new AlertDialog.Builder(this)
                    .SetTitle(string.Format(Resources.GetString(Resource.String.shareCreatorTitle), name))
                    .SetMessage(string.Format(Resources.GetString(Resource.String.shareCreatorMessage), name))
                    .SetPositiveButton("Yes, share", (a, b) => { Upload(preppedTasks, true); })
                    .SetNegativeButton("No", (a, b) => { Upload(preppedTasks, false); })
                    .SetCancelable(false)
                    .Show();
            }
            else
            {
                Upload(preppedTasks, false);
            }
        }

        private void Upload(List<AppTask> preppedTasks, bool shareWithCreator)
        {
            Random rand = new Random();
            AppDataUpload uploadData = new AppDataUpload
            {
                ItemId = rand.Next(),
                UploadRoute = $"api/CompletedTasks/Submit?activityId={learningActivity.Id}&shareWithCreator={shareWithCreator}&enteredName={enteredName}",
                Name = learningActivity.Name,
                Description = learningActivity.Description,
                ImageUrl = learningActivity.ImageUrl,
                UploadType = UploadType.Result,
                FilesJson = JsonConvert.SerializeObject(Storage.MakeUploads(preppedTasks)),
                JsonData = JsonConvert.SerializeObject(preppedTasks,
                    new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 5,
                        TypeNameHandling = TypeNameHandling.Auto
                    })
            };

            shouldSave = false;
            dbManager.AddUpload(uploadData);
            dbManager.DeleteProgress(learningActivity.Id);

            Intent intent = new Intent(this, typeof(UploadsActivity));
            StartActivity(intent);
            Finish();
        }

        /// <summary>
        /// When the Activity closes/goes into the background, save the user's progress
        /// </summary>
        protected override async void OnPause()
        {
            base.OnPause();

            if (shouldSave && learningActivity != null && adapter != null && adapter.Items != null)
            {
                (await Storage.GetDatabaseManager()).SaveActivityProgress(learningActivity, adapter.Items, enteredName);
            }
        }
    }
}