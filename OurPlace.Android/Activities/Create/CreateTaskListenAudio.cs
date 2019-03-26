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
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Views;
using Newtonsoft.Json;
using OurPlace.Common;
using OurPlace.Common.Models;
using System;
using System.Globalization;
using System.IO;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "New Task", Theme = "@style/OurPlaceActionBar")]
    public class CreateTaskListenAudio : AppCompatActivity
    {
        private EditText instructions;
        private TaskType taskType;
        private TextView fileTextView;
        private Button listenBtn;
        private Button addTaskBtn;
        private bool loaded = false;
        private const int ExistingReqCode = 22;
        private const int NewReqCode = 11;
        private const int PermReqCode = 33;
        private MediaPlayer player;

        private LearningTask newTask;
        private string editCachePath;
        private global::Android.Net.Uri outputFileUri;
        private bool editing = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CreateTaskListenAudio);

            fileTextView = FindViewById<TextView>(Resource.Id.chosenFile);
            instructions = FindViewById<EditText>(Resource.Id.taskInstructions);
            listenBtn = FindViewById<Button>(Resource.Id.listenBtn);
            addTaskBtn = FindViewById<Button>(Resource.Id.addTaskBtn);
            Button chooseFileBtn = FindViewById<Button>(Resource.Id.chooseFileBtn);
            addTaskBtn.Click += AddTaskBtn_Click;
            chooseFileBtn.Click += ChooseFileBtn_Click;
            listenBtn.Click += ListenBtn_Click;

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
                taskType = newTask.TaskType;
                instructions.Text = newTask.Description;
                addTaskBtn.SetText(Resource.String.saveChanges);
                fileTextView.SetText(Resource.String.createNewListenChosen);
                editCachePath = Path.Combine(
                    Common.LocalData.Storage.GetCacheFolder(null),
                    "editcache-" + DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss-fff"));

                if (!newTask.JsonData.StartsWith("upload"))
                {
                    File.Copy(newTask.JsonData, editCachePath, true);
                    Java.IO.File cachedFile = new Java.IO.File(editCachePath);
                    outputFileUri = global::Android.Net.Uri.FromFile(cachedFile);
                }
                else
                {
                    outputFileUri = global::Android.Net.Uri.Parse(newTask.JsonData);
                }

                AudioLoaded();
            }
            else
            {
                string jsonData = Intent.GetStringExtra("JSON") ?? "";
                taskType = JsonConvert.DeserializeObject<TaskType>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            }

            FindViewById<TextView>(Resource.Id.taskTypeNameText).Text = taskType.DisplayName;
            ImageViewAsync image = FindViewById<ImageViewAsync>(Resource.Id.taskIcon);
            AndroidUtils.LoadTaskTypeIcon(taskType, image);
        }

        protected override void OnResume()
        {
            base.OnResume();
            player = new MediaPlayer();
            player.Completion += Player_Completion;
        }

        private void ListenBtn_Click(object sender, EventArgs e)
        {
            if (player.IsPlaying)
            {
                player.Stop();
                player.Reset();
                listenBtn.Text = Resources.GetString(Resource.String.ListenBtn);
            }
            else
            {
                player.SetDataSource(outputFileUri.ToString().StartsWith("upload")
                    ? ServerUtils.GetUploadUrl(outputFileUri.ToString())
                    : outputFileUri.Path);

                player.Prepare();
                player.Start();
                listenBtn.Text = Resources.GetString(Resource.String.StopBtn);
            }
        }

        private void Player_Completion(object sender, EventArgs e)
        {
            player.Reset();
            if (listenBtn != null)
            {
                listenBtn.Text = Resources.GetString(Resource.String.ListenBtn);
            }
        }

        private void UpdateFiles()
        {
            if (outputFileUri != null && File.Exists(outputFileUri.Path))
            {
                File.Delete(outputFileUri.Path);
            }

            string filePath = Path.Combine(
                Common.LocalData.Storage.GetCacheFolder("created"),
                DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss-fff", CultureInfo.InvariantCulture)
                + ".mp4");

            Java.IO.File newFile = new Java.IO.File(filePath);
            outputFileUri = global::Android.Net.Uri.FromFile(newFile);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (!string.IsNullOrWhiteSpace(editCachePath) && File.Exists(editCachePath))
            {
                File.Delete(editCachePath);
            }
        }

        private void AudioLoaded()
        {
            fileTextView.SetText(Resource.String.createNewListenChosen);
            listenBtn.Visibility = global::Android.Views.ViewStates.Visible;
            loaded = true;
        }

        private void ChooseFileBtn_Click(object sender, EventArgs e)
        {
            new global::Android.Support.V7.App.AlertDialog.Builder(this)
            .SetMessage(Resource.String.createNewListenPopUp)
            .SetPositiveButton(Resource.String.createNewListenNew, (a, b) =>
            {

                string permission = global::Android.Manifest.Permission.RecordAudio;
                Permission currentPerm = ContextCompat.CheckSelfPermission(this, permission);
                if (currentPerm != Permission.Granted)
                {
                    AndroidUtils.CheckGetPermission(permission,
                    this, PermReqCode, base.Resources.GetString(Resource.String.permissionMicTitle),
                    base.Resources.GetString(Resource.String.permissionMicExplanation));
                }
                else
                {
                    StartRecordIntent();
                }

            })
            .SetNegativeButton(Resource.String.createNewListenExisting, (a, b) =>
            {
                Intent intent = new Intent();
                intent.SetAction(Intent.ActionGetContent);
                intent.SetType("audio/mpeg");
                base.StartActivityForResult(Intent.CreateChooser(
                    intent,
                    new Java.Lang.String(base.Resources.GetString(Resource.String.createNewListenAudioBtn))),
                    ExistingReqCode);
            })
            .Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == PermReqCode && grantResults[0] == Permission.Granted)
            {
                StartRecordIntent();
            }
        }

        private async void StartRecordIntent()
        {
            UpdateFiles();
            Intent myIntent = new Intent(this, typeof(ListenAudioRecordActivity));
            myIntent.PutExtra("JSON", outputFileUri.Path);
            StartActivityForResult(myIntent, NewReqCode);
        }

        protected override async void OnActivityResult(int requestCode, [GeneratedEnum] global::Android.App.Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == NewReqCode && resultCode == global::Android.App.Result.Ok && File.Exists(outputFileUri.Path))
            {
                AudioLoaded();
            }
            if (requestCode == ExistingReqCode && resultCode == global::Android.App.Result.Ok)
            {
                if (data != null && data.Data != null)
                {
                    UpdateFiles();

                    System.IO.Stream stream = base.ContentResolver.OpenInputStream(data.Data);
                    if (File.Exists(outputFileUri.Path))
                    {
                        File.Delete(outputFileUri.Path);
                    }

                    using (var fileStream = File.Create(outputFileUri.Path))
                    {
                        stream.CopyTo(fileStream);
                    }

                    AudioLoaded();
                }
            }
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

            if (!loaded)
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(Resource.String.ErrorTitle)
                .SetMessage(Resource.String.createNewListenNoFile)
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

            if (!editing || outputFileUri.Path != editCachePath)
            {
                newTask.JsonData = outputFileUri.Path;
            }

            string json = JsonConvert.SerializeObject(newTask, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                MaxDepth = 5
            });

            Intent myIntent = (editing) ?
                new Intent(this, typeof(CreateActivityOverviewActivity)) :
                new Intent(this, typeof(CreateChooseTaskTypeActivity));

            myIntent.PutExtra("JSON", json);
            SetResult(global::Android.App.Result.Ok, myIntent);
            Finish();
        }
    }
}