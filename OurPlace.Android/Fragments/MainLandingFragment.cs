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
using Android.Content;
using Android.Content.PM;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Location;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Android.Activities;
using OurPlace.Android.Adapters;
using OurPlace.Common.LocalData;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OurPlace.Android.Fragments
{
    public class MainLandingFragment : Fragment, GoogleApiClient.IConnectionCallbacks, GoogleApiClient.IOnConnectionFailedListener
    {
        private LearningActivitiesAdapter adapter;
        private RecyclerView recyclerView;
        private GridLayoutManager layoutManager;
        private SwipeRefreshLayout refresher;
        private bool viewLoaded;
        private bool loading = true;
        public static bool ForceRefresh;
        private const int PermReqId = 111;
        private GoogleApiClient googleApiClient;

        public override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Load from cached data from the database if available, 
            // just in case we can't contact the server
            List<ActivityFeedSection> cached = await ((MainActivity)Activity).GetCachedActivities(false);

            // Check for recently opened activities
            ActivityFeedSection recents = await LoadRecent();
            if (recents != null)
            {
                cached.Insert(0, recents);
            }

            var metrics = Resources.DisplayMetrics;
            var widthInDp = AndroidUtils.ConvertPixelsToDp(metrics.WidthPixels, Activity);

            int cols = Math.Max(widthInDp / 300, 1);

            adapter = new LearningActivitiesAdapter(cached, await ((MainActivity)Activity).GetDbManager());
            adapter.ItemClick += OnItemClick;

            if (savedInstanceState != null)
            {
                adapter.Data = JsonConvert.DeserializeObject<List<ActivityFeedSection>>(savedInstanceState.GetString("MAIN_ADAPTER_DATA"));
                adapter.NotifyDataSetChanged();
            }

            layoutManager = new GridLayoutManager(Activity, cols);
            layoutManager.SetSpanSizeLookup(new GridSpanner(adapter, cols));

            if (!AndroidUtils.IsGooglePlayServicesInstalled(Activity) || googleApiClient != null) return;

            googleApiClient = new GoogleApiClient.Builder(Activity)
                .AddConnectionCallbacks(this)
                .AddOnConnectionFailedListener(this)
                .AddApi(LocationServices.API)
                .Build();
            googleApiClient?.Connect();
        }

        public override void OnStop()
        {
            googleApiClient?.Disconnect();
            base.OnStop();
        }

        public override void OnResume()
        {
            base.OnResume();

            if (viewLoaded && ForceRefresh && !loading)
            {
                LoadData();
            }
        }

        public override bool UserVisibleHint
        {
            get => base.UserVisibleHint;

            set
            {
                if (value && viewLoaded && !loading && ForceRefresh)
                {
                    CheckLocationPermission();
                }

                base.UserVisibleHint = value;
            }
        }

        private void CheckLocationPermission()
        {
            loading = true;
            refresher.Refreshing = true;
            const string permission = global::Android.Manifest.Permission.AccessFineLocation;
            Permission currentPerm = ContextCompat.CheckSelfPermission(Activity, permission);
            if (currentPerm != Permission.Granted)
            {
                // Show an explanation of why it's needed if necessary
                if (ActivityCompat.ShouldShowRequestPermissionRationale(Activity, permission))
                {
                    ShowLocationPermExplanation(permission);
                }
                else
                {
                    // Check if we've asked before:
                    ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(Activity);
                    bool hasAsked = prefs.GetBoolean("has_asked_loc_perm", false);
                    
                    if(!hasAsked)
                    {
                        // We've not asked before, show an explanation
                        ShowLocationPermExplanation(permission);
                    }
                    else
                    {
                        // We've asked before, and they declined. Might be set to 'never ask again',
                        // so don't show explanation                     
                        RequestPermissions(new string[] { permission }, PermReqId);
                    }
                }
            }
            else
            {
                LoadData(true);
            }
        }

        private void ShowLocationPermExplanation(string permission)
        {
            global::Android.Support.V7.App.AlertDialog dialog = new global::Android.Support.V7.App.AlertDialog.Builder(Activity)
                        .SetTitle(Resources.GetString(Resource.String.permissionLocationActivitiesTitle))
                        .SetMessage(Resources.GetString(Resource.String.permissionLocationActivitiesExplanation))
                        .SetPositiveButton("Got it", (s, o) =>
                        {
                            RequestPermissions(new string[] { permission }, PermReqId);
                        })
                        .Create();
            dialog.Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode != PermReqId) return;

            // Save that we have already asked permission for location
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(Activity);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutBoolean("has_asked_loc_perm", true);
            editor.Apply();

            LoadData(grantResults[0] == Permission.Granted);
        }

        private async void LoadData(bool withLocation = false)
        {
            try
            {
                loading = true;
                refresher.Refreshing = true;

                // Default location values, ignored if 0,0
                double lat = 0;
                double lon = 0;

                if (googleApiClient != null && withLocation && googleApiClient.IsConnected)
                {
                    await Task.Run(() => {

                        global::Android.Locations.Location lastKnown = LocationServices.FusedLocationApi.GetLastLocation(googleApiClient);
                        if (lastKnown == null) return;

                        lat = lastKnown.Latitude;
                        lon = lastKnown.Longitude;

                    });
                }

                Common.ServerResponse<List<ActivityFeedSection>> results =
                    await Common.ServerUtils.Get<List<ActivityFeedSection>>($"/api/learningactivities/GetFeed?lat={lat}&lon={lon}");

                refresher.Refreshing = false;

                if (results == null)
                {
                    var suppress = AndroidUtils.ReturnToSignIn(Activity);
                    Toast.MakeText(Activity, Resource.String.ForceSignOut, ToastLength.Long).Show();
                    return;
                }

                ActivityFeedSection recent = await LoadRecent();

                if (!results.Success)
                {
                    //// if token invalid, return to sign-in 
                    if (Common.ServerUtils.CheckNeedsLogin(results.StatusCode))
                    {
                        var suppress = AndroidUtils.ReturnToSignIn(this.Activity);
                        return;
                    }

                    if (recent != null)
                    {
                        adapter.Data[0] = recent;
                        adapter.NotifyDataSetChanged();
                    }
                    
                    Toast.MakeText(Activity, Resource.String.ConnectionError, ToastLength.Long).Show();
                    return;
                }

                // Save this in the offline cache
                DatabaseManager dbManager = await ((MainActivity)Activity).GetDbManager();

                dbManager.currentUser.CachedActivitiesJson = JsonConvert.SerializeObject(results.Data,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 6
                    });
                dbManager.AddUser(dbManager.currentUser);

                // Check for recently opened activities
                
                if (recent != null)
                {
                    results.Data.Insert(0, recent);
                }

                adapter.Data = results.Data;
                adapter.NotifyDataSetChanged();
                ForceRefresh = false;
                viewLoaded = true;
                loading = false;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Load recently opened Activities
        /// </summary>
        /// <returns></returns>
        private async Task<ActivityFeedSection> LoadRecent()
        {
            List<LearningActivity> recentlyOpened = (await((MainActivity)Activity).GetDbManager()).GetActivities();
            if (recentlyOpened != null && recentlyOpened.Count > 0)
            {
                return new ActivityFeedSection
                {
                    Title = Resources.GetString(Resource.String.feedRecentTitle),
                    Description = Resources.GetString(Resource.String.feedRecentDesc),
                    Activities = recentlyOpened
                };
            }
            return null;
        }

        private void OnItemClick(object sender, int position)
        {
            LearningActivity act = adapter.GetItem(position);
            if (act != null)
            {
                viewLoaded = false;
                ((MainActivity)Activity).LaunchActivity(act);
            }
            else
            {
                Toast.MakeText(Activity, "ERROR", ToastLength.Short).Show();
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.MainLanding, container, false);
            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            refresher = view.FindViewById<SwipeRefreshLayout>(Resource.Id.refresher);
            refresher.Refresh += Refresher_Refresh;
            refresher.SetColorSchemeResources(
                Resource.Color.app_darkgreen,
                Resource.Color.app_green,
                Resource.Color.app_purple
                );

            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.recyclerView);
            recyclerView.SetLayoutManager(layoutManager);
            recyclerView.SetAdapter(adapter);

            viewLoaded = true;

            CheckLocationPermission();

            base.OnViewCreated(view, savedInstanceState);
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            if (adapter != null && adapter.Data.Count > 0)
            {
                outState.PutString("MAIN_ADAPTER_DATA", JsonConvert.SerializeObject(adapter.Data));
            }
        }

        private void Refresher_Refresh(object sender, EventArgs e)
        {
            CheckLocationPermission();
        }

        public void OnConnected(Bundle connectionHint)
        {
        }

        public void OnConnectionSuspended(int cause)
        {
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
        }
    }
}