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
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using FFImageLoading.Views;
using System;
using System.Threading;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Record Audio", Theme = "@style/OurPlaceActionBar", ScreenOrientation = ScreenOrientation.Portrait)]
    public class ListenAudioRecordActivity : AppCompatActivity
    {
        private ImageViewAsync image;
        private Button recordBtn;
        private TextView timer;
        private MediaRecorder recorder;
        private MediaPlayer player;
        private string filePath;
        private float lowAlpha = 0.25f;
        private Color defaultCol;
        private volatile bool recording = false;
        private Thread clockThread;
        private Button playBtn;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.RecordAudioActivity);

            recordBtn = FindViewById<Button>(Resource.Id.recordBtn);
            TextView taskDesc = FindViewById<TextView>(Resource.Id.taskDesc);

            string thisJsonData = Intent.GetStringExtra("JSON") ?? "";
            filePath = thisJsonData;

            timer = FindViewById<TextView>(Resource.Id.recTime);
            defaultCol = Color.Rgb(
                Color.GetRedComponent(timer.CurrentTextColor),
                Color.GetGreenComponent(timer.CurrentTextColor),
                Color.GetBlueComponent(timer.CurrentTextColor));

            image = FindViewById<ImageViewAsync>(Resource.Id.taskImage);
            image.Alpha = lowAlpha;

            recordBtn.Click += RecordBtn_Click;
            recordBtn.Text = Resources.GetString(Resource.String.StartBtn);
        }

        protected override void OnResume()
        {
            base.OnResume();
            recorder = new MediaRecorder();
            player = new MediaPlayer();
            player.Completion += Player_Completion;
        }

        protected override void OnPause()
        {
            base.OnPause();

            recorder.Release();
            recorder.Dispose();
            recorder = null;

            player.Release();
            player.Dispose();
            player = null;
        }

        private async void UpdateClock()
        {
            DateTime startedAt = DateTime.UtcNow;

            RunOnUiThread(() =>
            {
                timer.SetTextColor(Color.Red);
            });

            while (recording)
            {
                TimeSpan diff = DateTime.UtcNow - startedAt;

                RunOnUiThread(() =>
                {
                    timer.Text = diff.ToString(@"mm\:ss\:ff");
                });

                await System.Threading.Tasks.Task.Delay(15);
            }

            RunOnUiThread(() =>
            {
                timer.Text = "00:00:00";
                timer.SetTextColor(defaultCol);
            });
        }

        private void RecordBtn_Click(object sender, EventArgs e)
        {
            recording = !recording;

            if (recording)
            {
                // Start recording
                recorder.SetAudioSource(AudioSource.Mic);
                recorder.SetOutputFormat(OutputFormat.Mpeg4);
                recorder.SetAudioEncoder(AudioEncoder.Aac);
                recorder.SetAudioSamplingRate(44100);
                recorder.SetAudioEncodingBitRate(96000);
                recorder.SetOutputFile(filePath);
                recorder.Prepare();
                recorder.Start();

                recordBtn.Text = Resources.GetString(Resource.String.StopBtn);
                image.Alpha = 1f;
                clockThread = new Thread(UpdateClock);
                clockThread.Start();
            }
            else
            {
                // Stop recording
                recorder.Stop();
                recorder.Reset();

                recordBtn.Text = Resources.GetString(Resource.String.StartBtn);
                image.Alpha = lowAlpha;
                clockThread.Join();

                PlaybackAcceptPopup();
            }
        }

        private void PlaybackAcceptPopup()
        {
            View dialogLayout = LayoutInflater.Inflate(Resource.Layout.DialogButton, null);
            playBtn = dialogLayout.FindViewById<Button>(Resource.Id.dialogBtn);
            playBtn.Text = Resources.GetString(Resource.String.ListenBtn);
            playBtn.Click += (e, o) =>
            {

                if (player.IsPlaying)
                {
                    player.Stop();
                    player.Reset();
                    playBtn.Text = Resources.GetString(Resource.String.ListenBtn);
                }
                else
                {
                    player.SetDataSource(filePath);
                    player.Prepare();
                    player.Start();
                    playBtn.Text = Resources.GetString(Resource.String.StopBtn);
                }
            };

            global::Android.Support.V7.App.AlertDialog.Builder dialog = new global::Android.Support.V7.App.AlertDialog.Builder(this);
            dialog.SetTitle("Use this recording?");
            dialog.SetMessage("Do you want to use this recording, or try recording another clip?");
            dialog.SetView(dialogLayout);
            dialog.SetCancelable(false);
            dialog.SetNegativeButton("Record another", (s, e) =>
            {
                player.Stop();
                player.Reset();
            });
            dialog.SetPositiveButton("Use this", (s, e) =>
            {
                player.Stop();
                player.Reset();
                ReturnWithFile();
            });
            dialog.Show();
        }

        private void Player_Completion(object sender, EventArgs e)
        {
            player.Reset();
            if (playBtn != null)
            {
                playBtn.Text = Resources.GetString(Resource.String.ListenBtn);
            }
        }

        public void ReturnWithFile()
        {
            Intent myIntent = new Intent(this, typeof(CreateTaskListenAudio));
            myIntent.PutExtra("FILE_PATH", filePath);
            SetResult(global::Android.App.Result.Ok, myIntent);
            Finish();
        }
    }
}