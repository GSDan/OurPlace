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
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Views;
using Newtonsoft.Json;
using OurPlace.Android.Fragments;
using OurPlace.Common.Models;
using System;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "New Task", Theme = "@style/OurPlaceActionBar")]
    public class CreateTaskLocationHunt : AppCompatActivity, IOnMapReadyCallback
    {
        private EditText instructions;
        private TaskType taskType;
        private GoogleMap GMap;
        private LocationHuntLocation Selected;
        private ProgressDialog progDialog;
        private int permRequestCode = 111;
        private bool alreadyCentered = false;
        private ScrollView scrollView;

        private LearningTask newTask;
        private bool editing = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CreateTaskLocationHunt);
            instructions = FindViewById<EditText>(Resource.Id.taskInstructions);
            scrollView = FindViewById<ScrollView>(Resource.Id.scrollView);
            Button addTaskBtn = FindViewById<Button>(Resource.Id.addTaskBtn);
            addTaskBtn.Click += AddTaskBtn_Click;

            // Check if this is editing an existing task: if so, populate fields
            string editJson = Intent.GetStringExtra("EDIT") ?? "";
            newTask = JsonConvert.DeserializeObject<LearningTask>(editJson);

            if (newTask != null)
            {
                editing = true;
                taskType = newTask.TaskType;
                instructions.Text = newTask.Description;
                Selected = JsonConvert.DeserializeObject<LocationHuntLocation>(newTask.JsonData);
                addTaskBtn.SetText(Resource.String.saveChanges);
            }
            else
            {
                string jsonData = Intent.GetStringExtra("JSON") ?? "";
                taskType = JsonConvert.DeserializeObject<TaskType>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            }

            FindViewById<TextView>(Resource.Id.taskTypeNameText).Text = taskType.DisplayName;
            ImageViewAsync image = FindViewById<ImageViewAsync>(Resource.Id.taskIcon);
            ImageService.Instance.LoadUrl(taskType.IconUrl).IntoAsync(image);

            TouchableMapFragment mapFrag = (TouchableMapFragment)FragmentManager.FindFragmentById(Resource.Id.map);
            mapFrag.TouchUp += (sender, args) => scrollView.RequestDisallowInterceptTouchEvent(false);
            mapFrag.TouchDown += (sender, args) => scrollView.RequestDisallowInterceptTouchEvent(true);
            mapFrag.GetMapAsync(this);
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            GMap = googleMap;

            if (Selected != null)
            {
                // There's been a point added already!
                MarkerOptions opts = new MarkerOptions();
                opts.SetPosition(new LatLng(Selected.Lat, Selected.Long));
                GMap.Clear();
                GMap.AddMarker(opts);
            }

            string permission = global::Android.Manifest.Permission.AccessFineLocation;
            Permission currentPerm = ContextCompat.CheckSelfPermission(this, permission);
            if (currentPerm != Permission.Granted)
            {
                AndroidUtils.CheckGetPermission(permission,
                this, permRequestCode, Resources.GetString(Resource.String.permissionLocationTitle),
                Resources.GetString(Resource.String.permissionLocationExplanation));
            }
            else
            {
                GetLocation();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == permRequestCode)
            {
                if (grantResults[0] != Permission.Granted)
                {
                    Finish();
                    return;
                }
                GetLocation();
            }
        }

        private void GetLocation()
        {
            progDialog = ProgressDialog.Show(this, "Please Wait", "Getting your location", true);

            MapsInitializer.Initialize(this);
            GMap.MapType = GoogleMap.MapTypeHybrid;
            GMap.MyLocationEnabled = true;
            GMap.BuildingsEnabled = true;
            GMap.MapClick += GMap_MapClick;
            GMap.UiSettings.ScrollGesturesEnabled = true;
            GMap.MyLocationChange += GMap_MyLocationChange;
            GMap.MarkerClick += GoogleMap_MarkerClick;
        }

        private void GMap_MapClick(object sender, GoogleMap.MapClickEventArgs e)
        {
            Selected = new LocationHuntLocation(e.Point.Latitude, e.Point.Longitude, GMap.CameraPosition.Zoom, false);

            MarkerOptions opts = new MarkerOptions();
            opts.SetPosition(new LatLng(Selected.Lat, Selected.Long));
            GMap.Clear();
            GMap.AddMarker(opts);
        }

        private void GMap_MyLocationChange(object sender, GoogleMap.MyLocationChangeEventArgs e)
        {
            if (progDialog != null)
            {
                progDialog.Dismiss();
                progDialog = null;
            }

            if (alreadyCentered) return;

            alreadyCentered = true;
            GMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(e.Location.Latitude, e.Location.Longitude), 18f));
        }

        private void GoogleMap_MarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)
        {
            new global::Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle("Delete This Marker?")
                .SetMessage("Are you sure you want to delete this marker?")
                .SetPositiveButton("Delete", (a, b) =>
                {
                    e.Marker.Remove();
                    Selected = null;
                })
                .SetNegativeButton("Cancel", (a, b) => { })
                .Show();
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

            if (Selected == null)
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.ErrorTitle)
                    .SetMessage(Resource.String.createNewLocationHuntChooseLocation)
                    .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                    .Show();
                return;
            }

            if (newTask == null)
            {
                newTask = new LearningTask() { TaskType = taskType };
            }

            CheckBox allowMap = FindViewById<CheckBox>(Resource.Id.checkboxMapAvailable);
            Selected.MapAvailable = allowMap.Checked;

            newTask.Description = instructions.Text;
            newTask.JsonData = JsonConvert.SerializeObject(Selected);

            string json = JsonConvert.SerializeObject(newTask, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                MaxDepth = 5
            });

            Intent myIntent = (editing) ?
                new Intent(this, typeof(CreateManageTasksActivity)) :
                new Intent(this, typeof(CreateChooseTaskTypeActivity));

            myIntent.PutExtra("JSON", json);
            SetResult(global::Android.App.Result.Ok, myIntent);
            Finish();
        }
    }
}