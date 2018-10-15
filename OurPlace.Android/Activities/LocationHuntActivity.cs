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
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Location;
using Android.Graphics;
using Android.Locations;
using Android.Media;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Microsoft.AppCenter.Analytics;
using Newtonsoft.Json;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.Threading;

namespace OurPlace.Android.Activities
{
    [Activity(Label = "Find Location", Theme = "@style/OurPlaceActionBar", ScreenOrientation = ScreenOrientation.Portrait)]
    public class LocationHuntActivity : AppCompatActivity, GoogleApiClient.IConnectionCallbacks, GoogleApiClient.IOnConnectionFailedListener, global::Android.Gms.Location.ILocationListener
    {
        private LearningTask learningTask;
        private ImageView image;
        private TextView distanceText;
        private TextView accuracyText;
        private Color defaultCol;
        private readonly float lowAlpha = 0.1f;
        private GoogleApiClient googleApiClient;
        private LocationRequest locRequest;
        private Thread animThread;
        private volatile int distanceMetres = 0;
        private volatile bool shouldAnimate = false;
        private LocationHuntLocation target;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.LocationHunActivity);

            string thisJsonData = Intent.GetStringExtra("JSON") ?? "";
            learningTask = JsonConvert.DeserializeObject<LearningTask>(thisJsonData,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            SupportActionBar.Title = learningTask.Description;

            target = JsonConvert.DeserializeObject<LocationHuntLocation>(learningTask.JsonData);

            TextView taskDesc = FindViewById<TextView>(Resource.Id.taskDesc);
            taskDesc.Text = learningTask.Description;

            distanceText = FindViewById<TextView>(Resource.Id.distanceText);
            distanceText.Text = "Please wait";

            defaultCol = Color.Rgb(
                Color.GetRedComponent(distanceText.CurrentTextColor),
                Color.GetGreenComponent(distanceText.CurrentTextColor),
                Color.GetBlueComponent(distanceText.CurrentTextColor));

            accuracyText = FindViewById<TextView>(Resource.Id.accuracyText);
            accuracyText.Text = "Connecting";

            image = FindViewById<ImageView>(Resource.Id.taskImage);
            image.Alpha = lowAlpha;

            Button openMapButton = FindViewById<Button>(Resource.Id.openMapButton);
            openMapButton.Click += OpenMapButton_Click;
            openMapButton.Visibility = (target.MapAvailable == null || target.MapAvailable == true) 
                ? ViewStates.Visible : ViewStates.Gone;


            if(AndroidUtils.IsGooglePlayServicesInstalled(this) && googleApiClient == null)
            {
                googleApiClient = new GoogleApiClient.Builder(this)
                    .AddConnectionCallbacks(this)
                    .AddOnConnectionFailedListener(this)
                    .AddApi(LocationServices.API)
                    .Build();
                locRequest = new LocationRequest();
            }
        }

        private async void AnimateImage()
        {
            ToneGenerator toneG = new ToneGenerator(Stream.Music, 50);
            while (shouldAnimate)
            {
                if (googleApiClient.IsConnected)
                {
                    float opacity = 0.1f;

                    if (distanceMetres <= 10)
                    {
                        opacity = 1;
                    }
                    else if (distanceMetres < 100)
                    {
                        float calcOpacity = 1 - (float)distanceMetres / 100;
                        if (calcOpacity > opacity) opacity = calcOpacity;
                    }

                    image.Alpha = opacity;

                    long animDuration = Math.Max((long)Math.Min(1000, distanceMetres), 10);

                    ObjectAnimator scaleDown = ObjectAnimator.OfPropertyValuesHolder(image,
                            PropertyValuesHolder.OfFloat("scaleX", 1.2f),
                            PropertyValuesHolder.OfFloat("scaleY", 1.2f));
                    scaleDown.SetDuration(Math.Max(animDuration, 50));

                    scaleDown.RepeatCount = 1;
                    scaleDown.RepeatMode = ValueAnimatorRepeatMode.Reverse;

                    RunOnUiThread(() =>
                    {
                        scaleDown.Start();
                    });

                    toneG.StartTone(Tone.CdmaAlertCallGuard, 50);
                    await System.Threading.Tasks.Task.Delay((int)animDuration * 5); 
                }
            }
            toneG.Release();
        }

        protected override void OnResume()
        {
            base.OnResume();

            Console.WriteLine("OnResume, connecting");

            googleApiClient.Connect();            
        }

        protected override async void OnPause()
        {
            base.OnPause();

            if (googleApiClient.IsConnected)
            {
                // stop location updates, passing in the LocationListener
                await LocationServices.FusedLocationApi.RemoveLocationUpdates(googleApiClient, this);

                googleApiClient.Disconnect();
            }

            if (animThread != null)
            {
                shouldAnimate = false;
                animThread.Join();
                animThread = null;
            }
        }

        public void OnConnected(Bundle connectionHint)
        {
            Console.WriteLine("Google API client connected!");

            // Setting location priority to PRIORITY_HIGH_ACCURACY (100)
            locRequest.SetPriority(100);

            // Setting interval between updates, in milliseconds
            // NOTE: the default FastestInterval is 1 minute. If you want to receive location updates more than 
            // once a minute, you _must_ also change the FastestInterval to be less than or equal to your Interval
            locRequest.SetFastestInterval(500);
            locRequest.SetInterval(2000);

            // pass in a location request and LocationListener
            LocationServices.FusedLocationApi.RequestLocationUpdates(googleApiClient, locRequest, this);
        }

        public void OnConnectionSuspended(int cause)
        {
            Console.WriteLine("Google API client suspended!");
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
            Console.WriteLine("Google API client suspended!");
            Toast.MakeText(this, "Failed to connect", ToastLength.Long).Show();
        }

        public void OnLocationChanged(global::Android.Locations.Location location)
        {
            float[] results = new float[1];
            global::Android.Locations.Location.DistanceBetween(location.Latitude, location.Longitude,
                target.Lat, target.Long, results);

            distanceMetres = (int)results[0];

            string dist;

            if(distanceMetres > 1000)
            {
                dist = string.Format("{0} km", (results[0] / 1000).ToString("n2"));
            }
            else
            {
                dist = string.Format("{0} metres", distanceMetres);
            }

            distanceText.Text = string.Format("Distance: {0}", dist);
            accuracyText.Text = string.Format("Accuracy: {0} metres", location.Accuracy.ToString("n0"));

            if(animThread == null)
            {
                shouldAnimate = true;
                animThread = new Thread(AnimateImage);
                animThread.Start();
            }

            if(distanceMetres < 10)
            {
                Arrived();
            }
        }

        private void Arrived()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                {"TaskId", learningTask?.Id.ToString() }
            };
            Analytics.TrackEvent("LocationHuntActivity_Arrived", properties);

            new global::Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(base.Resources.GetString(Resource.String.locationHuntArrivedTitle))
                .SetMessage(base.Resources.GetString(Resource.String.locationHuntArrivedMessage))
                .SetPositiveButton("Got it!", (a, b) => {
                    Intent myIntent = new Intent(this, typeof(ActTaskListActivity));
                    myIntent.PutExtra("TASK_ID", learningTask.Id);
                    myIntent.PutExtra("COMPLETE", true);
                    SetResult(global::Android.App.Result.Ok, myIntent);
                    base.Finish();
                })
                .SetCancelable(false)
                .Show();
        }

        private void OpenMapButton_Click(object sender, EventArgs e)
        {
            global::Android.Net.Uri mapsIntentUri = global::Android.Net.Uri.Parse(
                string.Format("geo:{0},{1}?q={0},{1}(Target+Location)", target.Lat, target.Long));
            Intent mapIntent = new Intent(Intent.ActionView, mapsIntentUri);
            mapIntent.SetPackage("com.google.android.apps.maps");
            StartActivity(mapIntent);
        }

    }
}