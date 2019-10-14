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
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Views;
using Android.Widget;
using Com.Dekoservidoni.Omfm;
using Microsoft.AppCenter.Analytics;
using Newtonsoft.Json;
using OurPlace.Android.Activities;
using OurPlace.Android.Activities.Create;
using OurPlace.Android.Adapters;
using OurPlace.Common;
using OurPlace.Common.LocalData;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OurPlace.Common.Interfaces;

namespace OurPlace.Android.Fragments
{
    public class MainMyCreationsFragment : global::Android.Support.V4.App.Fragment, OneMoreFabMenu.IOptionsClick
    {
        private FeedItemsAdapter adapter;
        private RecyclerView recyclerView;
        private GridLayoutManager layoutManager;
        private TextView fabPrompt;
        private SwipeRefreshLayout refresher;
        private const int PermReqId = 111;
        private List<LearningActivity> unsubmittedActivities;
        private List<ActivityCollection> unsubmittedCollections;
        private TextView uploadsHint;
        private bool refreshingData = true;
        private bool viewLoaded;
        private Intent requiresStorageIntent;
        public static bool ForceRefresh { get; set; }

        public override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Load from cached data from the database if available, 
            // just in case we can't contact the server
            List<FeedSection> cached = await ((MainActivity)Activity).GetCachedActivities(false);

            var metrics = Resources.DisplayMetrics;
            var widthInDp = AndroidUtils.ConvertPixelsToDp(metrics.WidthPixels, Activity);
            int cols = Math.Max(widthInDp / 300, 1);

            adapter = new FeedItemsAdapter(cached, await ((MainActivity)Activity).GetDbManager());
            adapter.ItemClick += OnItemClick;
            layoutManager = new GridLayoutManager(Activity, cols);
            layoutManager.SetSpanSizeLookup(new GridSpanner(adapter, cols));
        }

        public override async void OnResume()
        {
            base.OnResume();

            if (viewLoaded && !refreshingData && ForceRefresh)
            {
                LoadRemoteData();
            }

            uploadsHint.Visibility = ((await ((MainActivity)Activity).GetDbManager()).GetUploadQueue().Any()) ?
                    ViewStates.Visible : ViewStates.Gone;
        }

        public override bool UserVisibleHint
        {
            get => base.UserVisibleHint;

            set
            {
                if (value && viewLoaded && !refreshingData && ForceRefresh)
                {
                    LoadRemoteData();
                }

                base.UserVisibleHint = value;
            }
        }

        private async void LoadRemoteData()
        {
            try
            {
                refreshingData = true;
                refresher.Refreshing = true;
                ForceRefresh = false;

                DatabaseManager dbManager = await ((MainActivity)Activity).GetDbManager();

                ServerResponse<List<LearningActivity>> results =
                    await ServerUtils.Get<List<LearningActivity>>(
                        "/api/learningactivities/getfromuser/?creatorId=" + dbManager.CurrentUser.Id);
                refresher.Refreshing = false;

                if (results == null)
                {
                    var suppress = AndroidUtils.ReturnToSignIn(Activity);
                    Toast.MakeText(Activity, Resource.String.ForceSignOut, ToastLength.Long).Show();
                    return;
                }

                if (!results.Success)
                {
                    Toast.MakeText(Activity, Resource.String.ConnectionError, ToastLength.Long).Show();
                    await LoadIntoFeed(null);
                    return;
                }

                // Save this in the offline cache
                dbManager.CurrentUser.RemoteCreatedActivitiesJson = JsonConvert.SerializeObject(results.Data,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 5
                    });
                dbManager.AddUser(dbManager.CurrentUser);

                List<LearningActivity> recentlyOpened = dbManager.GetActivities();
                foreach (LearningActivity cachedActivity in recentlyOpened)
                {
                    LearningActivity refreshedVersion = results.Data.FirstOrDefault(act => act.Id == cachedActivity.Id);
                    if (refreshedVersion != null &&
                        refreshedVersion.ActivityVersionNumber > cachedActivity.ActivityVersionNumber)
                    {
                        dbManager.DeleteCachedActivity(cachedActivity);
                        MainLandingFragment.ForceRefresh = true;
                    }
                }

                await LoadIntoFeed(results.Data.OrderByDescending(act => act.CreatedAt).ToList());

                refreshingData = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private async Task LoadIntoFeed(List<LearningActivity> remoteData)
        {
            List<FeedSection> feed = new List<FeedSection>();

            string unsubmittedCollectionsJson = (await ((MainActivity)Activity).GetCurrentUser()).LocalCreatedCollectionsJson;
            unsubmittedCollections = null;

            if (!string.IsNullOrWhiteSpace(unsubmittedCollectionsJson))
            {
                unsubmittedCollections = JsonConvert.DeserializeObject<List<ActivityCollection>>(
                    unsubmittedCollectionsJson,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 10
                    });
            }

            // Add a section to the feed if the user has activities which they didn't finish creating
            if (unsubmittedCollections != null && unsubmittedCollections.Count > 0)
            {
                feed.Add(new FeedSection
                {
                    Title = Resources.GetString(Resource.String.createdCollectionsLocalTitle),
                    Description = Resources.GetString(Resource.String.createdCollectionsLocalDesc),
                    Collections = unsubmittedCollections
                });
            }

            string unsubmittedActivitiesJson = (await ((MainActivity)Activity).GetCurrentUser()).LocalCreatedActivitiesJson;
            unsubmittedActivities = null;

            if (!string.IsNullOrWhiteSpace(unsubmittedActivitiesJson))
            {
                unsubmittedActivities = JsonConvert.DeserializeObject<List<LearningActivity>>(
                    unsubmittedActivitiesJson,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 10
                    });
            }

            // Add a section to the feed if the user has activities which they didn't finish creating
            if (unsubmittedActivities != null && unsubmittedActivities.Count > 0)
            {
                feed.Add(new FeedSection
                {
                    Title = Resources.GetString(Resource.String.createdActivitiesLocalTitle),
                    Description = Resources.GetString(Resource.String.createdActivitiesLocalDesc),
                    Activities = unsubmittedActivities
                });
            }

            // Add a section to the feed if the user has activities stored on the remote server
            if (remoteData != null && remoteData.Count > 0)
            {
                feed.Add(new FeedSection
                {
                    Title = Resources.GetString(Resource.String.createdFeedTitle),
                    Description = Resources.GetString(Resource.String.createdFeedDesc),
                    Activities = remoteData
                });
            }

            // Set up the adapter if needed, adding the feed data
            adapter.Data = feed;
            adapter.NotifyDataSetChanged();

            if (fabPrompt != null)
            {
                // Hide the fab tutorial if the user has already created an activity
                fabPrompt.Visibility = (adapter.Data.Count > 0) ? ViewStates.Gone : ViewStates.Visible;
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.MainMyActivities, container, false);
            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            uploadsHint = view.FindViewById<TextView>(Resource.Id.uploadsHint);
            fabPrompt = view.FindViewById<TextView>(Resource.Id.fabPrompt);
            refresher = view.FindViewById<SwipeRefreshLayout>(Resource.Id.refresher);
            refresher.Refresh += (a, e) => { LoadRemoteData(); };
            refresher.SetColorSchemeResources(
                Resource.Color.app_darkgreen,
                Resource.Color.app_green,
                Resource.Color.app_lightgreen,
                Resource.Color.app_lightergreen
            );

            OneMoreFabMenu fab = view.FindViewById<OneMoreFabMenu>(Resource.Id.fabMenu);
            fab.SetOptionsClick(this);

            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.recyclerView);
            recyclerView.SetLayoutManager(layoutManager);
            recyclerView.SetAdapter(adapter);

            viewLoaded = true;

            LoadRemoteData();
        }

        public void OnOptionClick(Java.Lang.Integer p0)
        {
            switch (Convert.ToInt32(p0))
            {
                case Resource.Id.fabCreateAct:
                    Analytics.TrackEvent("MainMyActivitiesFragment_StartCreate");
                    requiresStorageIntent = new Intent(Activity, typeof(CreateNewActivity));
                    LaunchWithStoragePermissions();
                    break;
                case Resource.Id.fabCreateColl:
                    Analytics.TrackEvent("MainMyActivitiesFragment_StartCreateCollection");
                    requiresStorageIntent = new Intent(Activity, typeof(CreateCollectionActivity));
                    LaunchWithStoragePermissions();
                    break;
            }
        }

        private void LaunchWithStoragePermissions()
        {
            const string permission = global::Android.Manifest.Permission.ReadExternalStorage;
            Permission currentPerm = ContextCompat.CheckSelfPermission(Activity, permission);

            const string writePerm = global::Android.Manifest.Permission.WriteExternalStorage;
            Permission currentWritePerm = ContextCompat.CheckSelfPermission(Activity, writePerm);

            if (currentPerm != Permission.Granted || currentWritePerm != Permission.Granted)
            {
                // Show an explanation of why it's needed if necessary
                if (ActivityCompat.ShouldShowRequestPermissionRationale(Activity, permission) || ActivityCompat.ShouldShowRequestPermissionRationale(Activity, writePerm))
                {
                    global::Android.Support.V7.App.AlertDialog dialog = new global::Android.Support.V7.App.AlertDialog.Builder(Activity)
                        .SetTitle(Resources.GetString(Resource.String.permissionFilesTitle))
                        .SetMessage(Resources.GetString(Resource.String.permissionFilesExplanation))
                        .SetPositiveButton("Got it", (s, o) =>
                        {
                            RequestPermissions(new string[] { permission, writePerm }, PermReqId);
                        })
                        .Create();
                    dialog.Show();
                }
                else
                {
                    // No explanation needed, just ask
                    RequestPermissions(new string[] { permission, writePerm }, PermReqId);
                }
            }
            else
            {
                StartStorageIntent();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == PermReqId && grantResults.All((p) => p == Permission.Granted))
            {
                StartStorageIntent();
            }
        }

        private void StartStorageIntent()
        {
            if (requiresStorageIntent != null)
            {
                StartActivity(requiresStorageIntent);
            }
        }

        private void OnItemClick(object sender, int position)
        {
            FeedItem chosen = adapter.GetItem(position);

            if (chosen == null)
            {
                Toast.MakeText(Activity, "ERROR", ToastLength.Short).Show();
                return;
            }

            if(chosen is LearningActivity chosenAct)
            {
                bool inProgress = unsubmittedActivities?.Exists((la) => chosenAct.Id == la.Id) ?? false;

                if (inProgress)
                {
                    EditCreation(chosenAct, true);
                }
                else
                {
                    new AlertDialog.Builder(Activity)
                        .SetTitle(chosen.Name)
                        .SetPositiveButton(Resource.String.EditBtn, (a, b) => { EditCreation(chosenAct, false); })
                        .SetNeutralButton(Resource.String.dialog_cancel, (a, b) => { })
                        .SetCancelable(true)
                        .SetMessage(Html.FromHtml(string.Format(Resources.GetString(Resource.String.createdActivityDialogMessage), chosenAct.InviteCode)))
                        .SetNegativeButton(Resource.String.createdActivityDialogOpen,
                        (a, b) => { ((MainActivity)Activity).LaunchActivity(chosenAct); })
                        .Show();
                }
            }
            else if(chosen is ActivityCollection chosenColl)
            {
                bool inProgress = unsubmittedCollections?.Exists((coll) => chosenColl.Id == coll.Id) ?? false;

                if(inProgress)
                {
                    EditCreation(chosenColl, true);
                }
            }
        }

        private void EditCreation(FeedItem chosen, bool isLocalOnly)
        {
            requiresStorageIntent = (chosen is LearningActivity)? 
                new Intent(Activity, typeof(CreateActivityOverviewActivity)) :
                new Intent(Activity, typeof(CreateCollectionOverviewActivity));
            string json = JsonConvert.SerializeObject(chosen, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                MaxDepth = 5
            });
            requiresStorageIntent.PutExtra("JSON", json);
            requiresStorageIntent.PutExtra("EDITING_SUBMITTED", !isLocalOnly);
            LaunchWithStoragePermissions();
        }

    }
}