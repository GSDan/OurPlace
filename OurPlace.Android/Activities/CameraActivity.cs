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
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Location;
using Android.OS;
using Android.Views;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Views;
using Microsoft.AppCenter.Analytics;
using Newtonsoft.Json;
using OurPlace.Android.Fragments;
using OurPlace.Common;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using static OurPlace.Common.LocalData.Storage;

namespace OurPlace.Android.Activities
{
    [Activity(Label = "CameraActivity")]//, ScreenOrientation = ScreenOrientation.Portrait)]
    public class CameraActivity : Activity, GoogleApiClient.IConnectionCallbacks, GoogleApiClient.IOnConnectionFailedListener
    {
        public AppTask learningTask;
        public int activityId;
        private GoogleApiClient googleApiClient;
        private LocationRequest locRequest;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.CameraActivity);

            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            learningTask = JsonConvert.DeserializeObject<AppTask>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            activityId = Intent.GetIntExtra("ACTID", -1);

            if (bundle == null)
            {
                if (learningTask.TaskType.IdName == "TAKE_VIDEO")
                {
                    RequestedOrientation = ScreenOrientation.Landscape;
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                    {
                        FragmentManager.BeginTransaction().Replace(Resource.Id.container, Camera2VideoFragment.NewInstance()).Commit();
                    }
                    else
                    {
                        FragmentManager.BeginTransaction().Replace(Resource.Id.container, Camera1VideoFragment.NewInstance()).Commit();
                    }
                }
                else
                {
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                    {
                        FragmentManager.BeginTransaction().Replace(Resource.Id.container, Camera2Fragment.NewInstance()).Commit();
                    }
                    else
                    {
                        FragmentManager.BeginTransaction().Replace(Resource.Id.container, Camera1Fragment.NewInstance()).Commit();
                    }
                }
            }

            if (!AndroidUtils.IsGooglePlayServicesInstalled(this) || googleApiClient != null)
            {
                return;
            }

            googleApiClient = new GoogleApiClient.Builder(this)
                .AddConnectionCallbacks(this)
                .AddOnConnectionFailedListener(this)
                .AddApi(LocationServices.API)
                .Build();

            locRequest = new LocationRequest();
        }

        public void LoadIfPhotoMatch(View view)
        {
            if (learningTask.TaskType.IdName != "MATCH_PHOTO")
            {
                return;
            }

            ImageViewAsync targetImageView = view.FindViewById<ImageViewAsync>(Resource.Id.targetPhoto);
            string imageUrl = learningTask.JsonData;
            AndroidUtils.LoadActivityImageIntoView(targetImageView, imageUrl, activityId, 500);
        }

        public void ReturnWithFile(string filePath)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                {"TaskId", learningTask.Id.ToString() }
            };
            Analytics.TrackEvent("CameraActivity_ReturnWithFile", properties);

            // add location to EXIF if it's known
            global::Android.Locations.Location loc = GetLastLocation();
            if (loc != null)
            {
                AndroidUtils.LocationToEXIF(filePath, loc);
            }

            Intent myIntent = new Intent(this, typeof(ActTaskListActivity));
            myIntent.PutExtra("TASK_ID", learningTask.Id);
            myIntent.PutExtra("FILE_PATH", filePath);
            SetResult(Result.Ok, myIntent);
            Finish();
        }

        protected override void OnResume()
        {
            base.OnResume();

            googleApiClient?.Connect();
        }

        protected override void OnPause()
        {
            base.OnPause();

            if (googleApiClient != null && googleApiClient.IsConnected)
            {
                googleApiClient.Disconnect();
            }
        }

        public void OnConnected(Bundle connectionHint)
        {
            Console.WriteLine("Google API client connected!");
        }

        public void OnConnectionSuspended(int cause)
        {
            Console.WriteLine("Google API client suspended!");
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
            Console.WriteLine("Google API client suspended!");
            Toast.MakeText(this, "Failed to get your location", ToastLength.Long).Show();
        }

        public global::Android.Locations.Location GetLastLocation()
        {
            if (googleApiClient != null && googleApiClient.IsConnected)
            {
                return LocationServices.FusedLocationApi.GetLastLocation(googleApiClient);
            }

            return null;
        }
    }
}