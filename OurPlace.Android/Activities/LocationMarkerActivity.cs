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
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using Microsoft.AppCenter.Analytics;
using Newtonsoft.Json;
using OurPlace.Common.Models;
using System.Collections.Generic;

namespace OurPlace.Android.Activities
{
    [Activity(Label = "Location Marker", Theme = "@style/OurPlaceActionBar", ScreenOrientation = ScreenOrientation.Portrait)]
    public class LocationMarkerActivity : AppCompatActivity, IOnMapReadyCallback
    {
        private GoogleMap GMap { get; set; }
        private List<Marker> selectedMarkers;
        private AppTask learningTask;
        private Button markButton;
        private Button finishButton;
        private ProgressDialog progDialog;
        private TextView locationCountText;
        private MapMarkerTaskData taskData;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.LocationMarkerActivity);

            string jsonData = Intent.GetStringExtra("JSON") ?? "";

            learningTask = JsonConvert.DeserializeObject<AppTask>(jsonData, 
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            taskData = JsonConvert.DeserializeObject<MapMarkerTaskData>(learningTask.JsonData);

            SupportActionBar.Title = learningTask.Description;
            selectedMarkers = new List<Marker>();

            MapFragment mapFrag = (MapFragment)FragmentManager.FindFragmentById(Resource.Id.map);
            mapFrag.GetMapAsync(this);

            progDialog = new ProgressDialog(this);
            progDialog.SetTitle(Resource.String.PleaseWait);
            progDialog.SetMessage(Resources.GetString(Resource.String.mapLoadingMessage)); // Why Xamarin, why??
            progDialog.SetCancelable(false);
            progDialog.Indeterminate = true;
            progDialog.SetButton(Resources.GetString(Resource.String.dialog_cancel), (a, b) => {
                Finish();
            });
            progDialog.Show();

            markButton = FindViewById<Button>(Resource.Id.markBtn);
            markButton.Click += MarkBtn_Click;
            finishButton = FindViewById<Button>(Resource.Id.finishBtn);
            finishButton.Click += FinishButton_Click;
            locationCountText = FindViewById<TextView>(Resource.Id.markersText);

            UpdateButton();
            UpdateText();
        }

        private void UpdateButton()
        {
            markButton.Enabled = CanPlaceMarkers();
            finishButton.Visibility = (selectedMarkers.Count > 0) ? global::Android.Views.ViewStates.Visible : global::Android.Views.ViewStates.Gone;
        }

        private bool CanPlaceMarkers()
        {
            return (selectedMarkers.Count < taskData.MaxNumMarkers) || (taskData.MaxNumMarkers == 0);
        }

        private void UpdateText()
        {
            locationCountText.Text = string.Format("{0}/{1} Locations Marked", selectedMarkers.Count, taskData.MaxNumMarkers);
        }

        private void StopDialog()
        {
            if (progDialog != null)
            {
                progDialog.Dismiss();
                progDialog = null;
            }
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            MapsInitializer.Initialize(Application.ApplicationContext);
            GMap = googleMap;
            GMap.MapType = GoogleMap.MapTypeHybrid;
            GMap.MyLocationEnabled = true;

            GMap.MyLocationChange += GMap_MyLocationChange;
            googleMap.MarkerClick += GoogleMap_MarkerClick;
            googleMap.MapClick += GoogleMap_MapClick;

            // Load previously placed locations
            List<Map_Location> existing = JsonConvert.DeserializeObject<List<Map_Location>>(learningTask.CompletionData.JsonData);
            if (existing != null)
            {
                foreach (Map_Location loc in existing)
                {
                    AddMarker(loc);
                }
                learningTask.CompletionData.JsonData = "";
            }
        }

        private void GoogleMap_MapClick(object sender, GoogleMap.MapClickEventArgs e)
        {
            if(!taskData.UserLocationOnly)
            {
                if(CanPlaceMarkers())
                {
                    Map_Location selected = new Map_Location(
                        e.Point.Latitude,
                        e.Point.Longitude,
                        GMap.CameraPosition.Zoom);
                                        AddMarker(selected);
                }
                else
                {
                    string maxMessage = string.Format(Resources.GetString(Resource.String.ChosenMaxLocations),
                        taskData.MaxNumMarkers, 
                        (taskData.MaxNumMarkers > 1) ? "s" : "");
                    Toast.MakeText(this, maxMessage, ToastLength.Long).Show();
                }
            }
            else if(CanPlaceMarkers())
            {
                Toast.MakeText(this, Resources.GetString(Resource.String.UserLocationOnly), ToastLength.Short).Show();
            }
        }

        private void GMap_MyLocationChange(object sender, GoogleMap.MyLocationChangeEventArgs e)
        {
            StopDialog();
            GMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(e.Location.Latitude, e.Location.Longitude), 18f));
        }

        private void GoogleMap_MarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)
        {
            new global::Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle("Delete This Marker?")
                .SetMessage("Are you sure you want to delete this marker? You can set it again by returning to the location.")
                .SetPositiveButton("Delete", (a, b) =>
                {
                    for (int i = 0; i < selectedMarkers.Count; i++)
                    {
                        if (selectedMarkers[i].Id == e.Marker.Id)
                        {
                            e.Marker.Remove();
                            selectedMarkers.RemoveAt(i);
                            break;
                        }
                    }

                    UpdateText();
                    UpdateButton();
                })
                .SetNegativeButton("Cancel", (a,b) =>{ })
                .Show();
        }

        private LatLng GetCenterPoint(LatLng lhs, LatLng rhs)
        {
            LatLngBounds.Builder bounds = new LatLngBounds.Builder();
            bounds.Include(lhs);
            bounds.Include(rhs);
            return bounds.Build().Center;
        }

        private void AddMarker(Map_Location newLoc)
        {
            MarkerOptions opts = new MarkerOptions();
            opts.SetPosition(new LatLng(newLoc.Lat, newLoc.Long));
            selectedMarkers.Add(GMap.AddMarker(opts));

            UpdateText();
            UpdateButton();
        }

        private void MarkBtn_Click(object sender, System.EventArgs e)
        {
            Map_Location selected = new Map_Location(
                GMap.MyLocation.Latitude, 
                GMap.MyLocation.Longitude, 
                GMap.CameraPosition.Zoom
                );
            AddMarker(selected);
        }

        private void FinishButton_Click(object sender, System.EventArgs e)
        {
            Map_Location[] locs = new Map_Location[selectedMarkers.Count];
           for (int i = 0; i < selectedMarkers.Count; i++)
           {
               locs[i] = new Map_Location(selectedMarkers[i].Position.Latitude, selectedMarkers[i].Position.Longitude, 15);
           }
           string locJson = JsonConvert.SerializeObject(locs);

           if (progDialog != null) progDialog.Dismiss();

            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                {"TaskId", learningTask.Id.ToString() },
                {"NumLocs", locs.Length.ToString()}
            };
            Analytics.TrackEvent("LocationMarkerActivity_Finish", properties);

            Intent myIntent = new Intent(this, typeof(ActTaskListActivity));
           myIntent.PutExtra("TASK_ID", learningTask.Id);
           myIntent.PutExtra("LOCATIONS", locJson);
            SetResult(global::Android.App.Result.Ok, myIntent);
           Finish();
        }
    }
}