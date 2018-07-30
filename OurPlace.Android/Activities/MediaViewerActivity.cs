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
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Common.Models;

namespace OurPlace.Android.Activities
{
    [Activity(Theme = "@style/OurPlaceActionBar", ParentActivity = typeof(ActTaskListActivity))]
    public class MediaViewerActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.MediaViewerActivity);

            string json = Intent.GetStringExtra("JSON") ?? "";
            int resIndex = Intent.GetIntExtra("RES_INDEX", -1);

            if (string.IsNullOrWhiteSpace(json) || resIndex == -1) return;

            AppTask thisTask = JsonConvert.DeserializeObject<AppTask>(json);

            SupportActionBar.Show();
            SupportActionBar.Title = thisTask.Description;

            string[] results = JsonConvert.DeserializeObject<string[]>(thisTask.CompletionData.JsonData);

            if(thisTask.TaskType.IdName == "TAKE_VIDEO")
            {
                VideoView videoView = FindViewById<VideoView>(Resource.Id.videoView);
                var uri = global::Android.Net.Uri.Parse(results[resIndex]);
                videoView.SetVideoURI(uri);
                videoView.Start();
            }
            
        }
    }
}