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
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.Widget;
using Android.Widget;
using FFImageLoading;
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
using System.IO;
using System.Threading.Tasks;

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
        private MediaPlayer player;
        private global::Android.Support.V7.App.AlertDialog playerDialog;
        private DatabaseManager dbManager;
        private readonly int mediaPlayerReqCode = 222;
        private string enteredName = "";
        private int permReqId = 111;
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
                alert.SetOnDismissListener(new OnDismissListener(() => { Finish(); }));
                alert.Show();
                return;
            }

            dbManager = await Storage.GetDatabaseManager();

            // Load this activity's progress from the database if available
            ActivityProgress progress = dbManager.GetProgress(learningActivity.Id);
            List<AppTask> appTasks = null;

            try
            {
                if (progress != null)
                {
                    enteredName = progress.EnteredUsername;
                    appTasks = JsonConvert.DeserializeObject<List<AppTask>>(progress.AppTaskJson);
                       // new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
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
                appTasks = new List<AppTask>();
                foreach (LearningTask t in learningActivity.LearningTasks)
                {
                    appTasks.Add(new AppTask(t));
                }
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

            if (learningActivity.RequireUsername && string.IsNullOrWhiteSpace(enteredName))
            {
                ShowNameEntry();
            }
            else if (!string.IsNullOrWhiteSpace(enteredName))
            {
                adapter.UpdateNames(enteredName);
            }
        }

        private void ShowNameEntry()
        {
            global::Android.Support.V7.App.AlertDialog.Builder builder = new global::Android.Support.V7.App.AlertDialog.Builder(this);
            builder.SetTitle(Resource.String.actEnterUsernameTitle);

            int px = (int)global::Android.Util.TypedValue.ApplyDimension(
                global::Android.Util.ComplexUnitType.Dip, 16, base.Resources.DisplayMetrics);

            TextView message = new TextView(this);
            message.SetText(Resource.String.actEnterUsername);
            EditText nameInput = new EditText(this);
            nameInput.Text = enteredName;

            LinearLayout dialogLayout = new LinearLayout(this);
            dialogLayout.Orientation = global::Android.Widget.Orientation.Vertical;
            dialogLayout.AddView(message);
            dialogLayout.AddView(nameInput);
            dialogLayout.SetPadding(px, px, px, px);

            builder.SetView(dialogLayout);
            builder.SetCancelable(false);
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
                    dbManager.SaveActivityProgress(learningActivity, adapter.items, enteredName);
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
                await AndroidUtils.PrepActivityFiles(this, learningActivity);
            }
            catch (Exception e)
            {
                Toast.MakeText(this, string.Format("{0}: {1}",
                    GetString(Resource.String.ErrorTitle), e.Message), ToastLength.Long).Show();
                Finish();
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

            if (learningActivity == null || adapter == null || adapter.items == null)
            {
                Finish();
            }
        }

        private void Adapter_SpeakText(object sender, int position)
        {
            if (adapter.ItemCount <= position) return;

            string toRead = (position > 0) ?
                adapter.items[position].Description : learningActivity.Description;

            tts.ReadText(toRead);
        }

        private void Adapter_EditName(object sender, int position)
        {
            ShowNameEntry();
        }

        private void Adapter_PlayAudio(string filepath, string description, int taskId)
        {
            global::Android.Support.V7.App.AlertDialog.Builder dialog = new global::Android.Support.V7.App.AlertDialog.Builder(this);
            dialog.SetMessage(description);
            dialog.SetCancelable(false);
            dialog.SetNegativeButton(Resource.String.StopBtn, (s, e) =>
            {
                Player_Clean();
            });
            playerDialog = dialog.Show();

            if (player == null)
            {
                player = new MediaPlayer();
                player.Completion += Player_Completion;
            }
            player.SetDataSource(filepath);
            player.Prepare();
            player.Start();

            adapter.OnGenericTaskReturned(taskId, true);

        }

        private void Player_Completion(object sender, EventArgs e)
        {
            Player_Clean();
        }

        private void Player_Clean()
        {
            if (player != null)
            {
                if (player.IsPlaying) player.Stop();
                player.Release();
                player.Dispose();
                player = null;
            }
            if (playerDialog != null)
            {
                playerDialog.Dismiss();
            }
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
            ProgressDialog prog = new ProgressDialog(this);
            prog.SetMessage(Resources.GetString(Resource.String.PleaseWait));
            prog.Show();
            ServerResponse<string> resp = await ServerUtils.Post<string>("/api/learningactivities/approve?id=" + learningActivity.Id, null);
            prog.Dismiss();

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

            adapter.items.RemoveAt(0);
            adapter.curator = false;
            learningActivity.Approved = true;

            dbManager.SaveActivityProgress(learningActivity, adapter.items, enteredName);

            adapter.NotifyDataSetChanged();
        }

        private async void DeleteActivity()
        {
            ProgressDialog prog = new ProgressDialog(this);
            prog.SetMessage(Resources.GetString(Resource.String.PleaseWait));
            prog.Show();
            ServerResponse<string> resp = await ServerUtils.Delete<string>("/api/learningactivities?id=" + learningActivity.Id);
            prog.Dismiss();

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
            if (adapter.items[position].TaskType.IdName == "ENTER_TEXT")
            {
                string entered = ((EditText)sender).Text;
                adapter.items[position].CompletionData.JsonData = entered;
                adapter.items[position].IsCompleted = !string.IsNullOrWhiteSpace(entered);
                if (!adapter.onBind)
                {
                    adapter.NotifyItemChanged(adapter.ItemCount - 1);
                }
            }
        }

        private void ShowMedia(int taskIndex, int pathIndex)
        {
            Intent viewActivity = new Intent(this, typeof(MediaViewerActivity));
            viewActivity.PutExtra("RES_INDEX", pathIndex);
            viewActivity.PutExtra("JSON", JsonConvert.SerializeObject(adapter.items[taskIndex]));
            StartActivityForResult(viewActivity, mediaPlayerReqCode);

            //string[] paths = JsonConvert.DeserializeObject<string[]>(
            //    adapter.items[taskIndex].CompletionData.JsonData);

            //if (paths == null || paths.Length <= pathIndex || !File.Exists(paths[pathIndex]))
            //{
            //    Toast.MakeText(this, "Problem accessing file", ToastLength.Short).Show();
            //    return;
            //}

            //try
            //{
            //    if (adapter.items[taskIndex].TaskType.IdName == "TAKE_VIDEO")
            //    {
            //        global::Android.Net.Uri uri = global::Android.Net.Uri.Parse(paths[pathIndex]);
            //        Intent intent = new Intent(Intent.ActionView, uri);
            //        intent.SetDataAndType(uri, "video/mp4");
            //        StartActivity(intent);
            //    }
            //    else if (adapter.items[taskIndex].TaskType.IdName == "REC_AUDIO")
            //    {
            //        global::Android.Net.Uri uri = FileProvider.GetUriForFile(this, base.ApplicationContext.PackageName + ".provider", new Java.IO.File(paths[pathIndex]));
            //        Intent intent = new Intent(Intent.ActionView, uri);
            //        intent.SetDataAndType(uri, "audio/mp4");
            //        intent.SetFlags(ActivityFlags.GrantReadUriPermission);
            //        StartActivity(intent);
            //    }
            //}
            //catch (ActivityNotFoundException)
            //{
            //    // The user doesn't have any apps installed to play audio/video files
            //    new global::Android.Support.V7.App.AlertDialog.Builder(this)
            //        .SetTitle(Resource.String.noAppErrTitle)
            //        .SetMessage(Resource.String.noAppErrMessage)
            //        .SetPositiveButton(Resource.String.dialog_ok, (e, i) => { })
            //        .Show();
            //}

        }

        // Called when the user has given/denied permission
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode != permReqId)
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
                return;
            }

            bool hasPerm = true;

            for (int i = 0; i < grantResults.Length; i++)
            {
                if (grantResults[i] != Permission.Granted)
                {
                    hasPerm = false;
                    break;
                }
            }

            if (hasPerm)
            {
                StartActivityForResult(lastReqIntent, 111);
            }
            else
            {
                Toast.MakeText(this, Resources.GetString(Resource.String.permissionRefused), ToastLength.Long).Show();
            }
        }

        private async void OnItemClick(object sender, int position)
        {
            // If Finish button clicked
            if (position == adapter.ItemCount - 1)
            {
                await PackageForUpload();
                return;
            }

            string json = JsonConvert.SerializeObject(adapter.items[position],
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    MaxDepth = 5
                });

            string taskType = adapter.items[position].TaskType.IdName;

            if (taskType == "TAKE_VIDEO" || taskType == "TAKE_PHOTO" || taskType == "MATCH_PHOTO")
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

                if (taskType == "TAKE_VIDEO")
                {
                    perms.Add(global::Android.Manifest.Permission.RecordAudio);
                    titles.Add(base.Resources.GetString(Resource.String.permissionMicTitle));
                    explanations.Add(base.Resources.GetString(Resource.String.permissionMicExplanation));
                }

                AndroidUtils.CallWithPermission(perms.ToArray(), titles.ToArray(), explanations.ToArray(),
                    lastReqIntent, adapter.items[position].Id, permReqId, this);
            }
            else if (taskType == "DRAW" || taskType == "DRAW_PHOTO")
            {
                Intent drawActivity = new Intent(this, typeof(DrawingActivity));
                drawActivity.PutExtra("JSON", json);

                if (taskType == "DRAW_PHOTO" && adapter.items[position].JsonData.StartsWith("TASK::", StringComparison.OrdinalIgnoreCase))
                {
                    int id = -1;
                    int.TryParse(adapter.items[position].JsonData.Substring(6), out id);
                    string[] paths = JsonConvert.DeserializeObject<string[]>(adapter.GetTaskWithId(id).CompletionData.JsonData);
                    drawActivity.PutExtra("PREVIOUS_PHOTO", paths[0]);
                }

                StartActivityForResult(drawActivity, adapter.items[position].Id);
            }
            else if (taskType == "MAP_MARK")
            {
                lastReqIntent = new Intent(this, typeof(LocationMarkerActivity));
                lastReqIntent.PutExtra("JSON", json);

                AndroidUtils.CallWithPermission(new string[] { global::Android.Manifest.Permission.AccessFineLocation },
                    new string[] { base.Resources.GetString(Resource.String.permissionLocationTitle)},
                    new string[] { base.Resources.GetString(Resource.String.permissionLocationExplanation)},
                    lastReqIntent, adapter.items[position].Id, permReqId, this);
            }
            else if (taskType == "LOC_HUNT")
            {
                lastReqIntent = new Intent(this, typeof(LocationHuntActivity));
                lastReqIntent.PutExtra("JSON", json);

                AndroidUtils.CallWithPermission(new string[] { global::Android.Manifest.Permission.AccessFineLocation },
                    new string[] { base.Resources.GetString(Resource.String.permissionLocationTitle) },
                    new string[] { base.Resources.GetString(Resource.String.permissionLocationExplanation) },
                    lastReqIntent, adapter.items[position].Id, permReqId, this);
            }
            else if (taskType == "REC_AUDIO")
            {
                lastReqIntent = new Intent(this, typeof(RecordAudioActivity));
                lastReqIntent.PutExtra("JSON", json);

                AndroidUtils.CallWithPermission(new string[] { global::Android.Manifest.Permission.RecordAudio },
                    new string[] { base.Resources.GetString(Resource.String.permissionMicTitle) },
                    new string[] { base.Resources.GetString(Resource.String.permissionMicExplanation) },
                    lastReqIntent, adapter.items[position].Id, permReqId, this);
            }
            else if (taskType == "LISTEN_AUDIO")
            {
                string localRes = Storage.GetCacheFilePath(
                    adapter.items[position].JsonData,
                    learningActivity.Id,
                    ServerUtils.GetFileExtension(taskType));

                if (File.Exists(localRes))
                {
                    Adapter_PlayAudio(localRes, adapter.items[position].Description, adapter.items[position].Id);
                }
                else
                {
                    Toast.MakeText(this, Resource.String.ErrorTitle, ToastLength.Short).Show();
                }

            }
            else if (taskType == "INFO")
            {
                AdditionalInfoData data = JsonConvert.DeserializeObject<AdditionalInfoData>(adapter.items[position].JsonData);
                global::Android.Net.Uri uri = global::Android.Net.Uri.Parse(data.ExternalUrl);
                Intent intent = new Intent(Intent.ActionView, uri);
                StartActivity(intent);
            }
            else
            {
                Toast.MakeText(this, "Unknown task type! :(", ToastLength.Short).Show();
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

            if (data == null || resultCode != Result.Ok) return;

            int taskId = data.GetIntExtra("TASK_ID", -1);
            int resIndex = data.GetIntExtra("RES_INDEX", -1);

            // deleting from MediaViewerActivity
            if (data.GetBooleanExtra("IS_DELETE", false) && taskId != -1 && resIndex != -1)
            {
                adapter.DeleteFile(taskId, resIndex);
                return;
            }

            string newFile = data.GetStringExtra("FILE_PATH");
            string mapLocs = data.GetStringExtra("LOCATIONS");
            bool isPoly = data.GetBooleanExtra("IS_POLYGON", false);
            bool complete = data.GetBooleanExtra("COMPLETE", false);

            if (resultCode == global::Android.App.Result.Ok && taskId != -1 && !string.IsNullOrWhiteSpace(newFile))
            {
                string taskType = adapter.GetTaskWithId(taskId).TaskType.IdName;

                adapter.OnFileReturned(taskId, newFile,
                    (taskType == "TAKE_PHOTO" || taskType == "MATCH_PHOTO"));
            }
            else if (!string.IsNullOrWhiteSpace(mapLocs))
            {
                adapter.OnMapReturned(taskId, mapLocs, isPoly);
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

            dbManager.SaveActivityProgress(learningActivity, adapter.items, enteredName);
        }

        /// <summary>
        /// Package the entered/created data up for storage
        /// </summary>
        public async Task PackageForUpload()
        {
            List<AppTask> preppedTasks = new List<AppTask>();
            foreach (AppTask t in adapter.items)
            {
                if (t == null) continue;

                preppedTasks.Add(Storage.PrepForUpload(t));
            }

            await ImageService.Instance.InvalidateCacheAsync(FFImageLoading.Cache.CacheType.All);

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
                UploadRoute = string.Format("api/CompletedTasks/Submit?activityId={0}&shareWithCreator={1}&enteredName={2}",
                                            learningActivity.Id, shareWithCreator, enteredName),
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

            Player_Clean();

            if (shouldSave && learningActivity != null && adapter != null && adapter.items != null)
            {
                (await Storage.GetDatabaseManager()).SaveActivityProgress(learningActivity, adapter.items, enteredName);
            }
        }
    }
}