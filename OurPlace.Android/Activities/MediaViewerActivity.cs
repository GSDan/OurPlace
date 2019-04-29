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
using Android.Media;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Views;
using Microsoft.AppCenter.Analytics;
using Newtonsoft.Json;
using OurPlace.Android.Misc;
using OurPlace.Common;
using OurPlace.Common.LocalData;
using OurPlace.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace OurPlace.Android.Activities
{
    [Activity(Theme = "@style/OurPlaceActionBar", ParentActivity = typeof(ActTaskListActivity))]
    public class MediaViewerActivity : AppCompatActivity, MediaPlayer.IOnPreparedListener
    {
        private int taskId;
        private int resIndex;
        private string taskType;

        private AlwaysVisibleMediaController mediaController;
        private VideoView videoView;
        private bool isPaused;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.MediaViewerActivity);

            string json = Intent.GetStringExtra("JSON") ?? "";
            int activityId = Intent.GetIntExtra("ACT_ID", -1);
            resIndex = Intent.GetIntExtra("RES_INDEX", -1);

            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            AppTask thisTask = JsonConvert.DeserializeObject<AppTask>(json);

            taskId = thisTask.Id;

            SupportActionBar.Show();
            SupportActionBar.Title = thisTask.Description;

            string[] results = null;

            if (thisTask.CompletionData?.JsonData != null)
            {
                results = JsonConvert.DeserializeObject<string[]>(thisTask.CompletionData?.JsonData);
            }

            taskType = thisTask.TaskType.IdName;

            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                {"TaskId", taskId.ToString() },
                {"TaskType", taskType }
            };
            Analytics.TrackEvent("MediaViewerActivity", properties);

            if (new string[] { "TAKE_VIDEO", "REC_AUDIO", "LISTEN_AUDIO" }.Contains(taskType))
            {
                if (taskType == "LISTEN_AUDIO" || taskType == "REC_AUDIO")
                {
                    ImageViewAsync imageView = FindViewById<ImageViewAsync>(Resource.Id.speakerImage);
                    imageView.Visibility = ViewStates.Visible;
                }

                global::Android.Net.Uri uri = null;

                if (taskType == "LISTEN_AUDIO")
                {
                    string localRes = Storage.GetCacheFilePath(
                        thisTask.JsonData,
                        activityId,
                        ServerUtils.GetFileExtension(taskType));
                    uri = global::Android.Net.Uri.Parse(localRes);
                }
                else
                {
                    if (results != null)
                    {
                        uri = global::Android.Net.Uri.Parse(results[resIndex]);
                    }
                }

                // easiest way to get audio playback controls is to use a videoview
                videoView = FindViewById<VideoView>(Resource.Id.videoView);
                videoView.Visibility = ViewStates.Visible;
                videoView.SetOnPreparedListener(this);
                videoView.SetVideoURI(uri);

                mediaController = new AlwaysVisibleMediaController(this, Finish);
                mediaController.SetAnchorView(videoView);
                videoView.SetMediaController(mediaController);
            }
            else if (new string[] { "DRAW", "DRAW_PHOTO", "TAKE_PHOTO", "MATCH_PHOTO" }.Contains(taskType))
            {
                ImageViewAsync imageView = FindViewById<ImageViewAsync>(Resource.Id.imageView);
                imageView.Visibility = ViewStates.Visible;
                if (results != null)
                {
                    ImageService.Instance.LoadFile(results[resIndex]).FadeAnimation(true).Into(imageView);
                }

            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            if (videoView == null || !videoView.CanPause())
            {
                return;
            }

            videoView.Pause();
            isPaused = true;
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (videoView == null || !isPaused)
            {
                return;
            }

            videoView.Resume();
            isPaused = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (videoView == null)
            {
                return;
            }

            if (videoView.IsPlaying)
            {
                videoView.StopPlayback();
            }
            videoView = null;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            if (taskType != "LISTEN_AUDIO")
            {
                MenuInflater.Inflate(Resource.Menu.MediaViewerMenu, menu);
            }
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId != Resource.Id.menudelete)
            {
                return base.OnOptionsItemSelected(item);
            }

            new global::Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(Resource.String.deleteTitle)
                .SetMessage(Resource.String.deleteMessage)
                .SetNegativeButton(Resource.String.dialog_cancel, (a, e) =>
                {
                })
                .SetPositiveButton(Resource.String.DeleteBtn, (a, e) =>
                {
                    ReturnToDelete();
                })
                .Show();

            return true;

        }

        public void OnPrepared(MediaPlayer mp)
        {
            mp.Looping = true;
            videoView.Start();
            mediaController.Show(0);
        }

        public void ReturnToDelete()
        {
            Intent myIntent = new Intent(this, typeof(ActTaskListActivity));
            myIntent.PutExtra("IS_DELETE", true);
            myIntent.PutExtra("TASK_ID", taskId);
            myIntent.PutExtra("RES_INDEX", resIndex);
            SetResult(Result.Ok, myIntent);
            Finish();
        }
    }
}