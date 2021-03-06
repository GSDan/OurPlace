﻿#region copyright
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
using Android.OS;
using Android.Preferences;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Microsoft.AppCenter.Analytics;
using Newtonsoft.Json;
using OurPlace.Android.Activities.Create;
using OurPlace.Android.Adapters;
using OurPlace.Android.Fragments;
using OurPlace.Common;
using OurPlace.Common.LocalData;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ZXing.Mobile;

namespace OurPlace.Android.Activities
{
    [Activity(Label = "OurPlace", Theme = "@style/OurPlaceActionBar", Icon = "@mipmap/ic_launcher", LaunchMode = LaunchMode.SingleTask)]
    [IntentFilter(new[] { Intent.ActionView }, DataScheme = "https", DataHost = ConfidentialData.hostname, DataPath = "/app/activity", Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault })]
    [IntentFilter(new[] { Intent.ActionView }, DataScheme = "http", DataHost = ConfidentialData.hostname, DataPath = "/app/activity", Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault })]
    [IntentFilter(new[] { Intent.ActionView }, DataScheme = "parklearn", DataHost = "activity", Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault })]
    public class MainActivity : AppCompatActivity
    {

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            PagerAdapter pageAdapter = new PagerAdapter(SupportFragmentManager, this);
            var pager = FindViewById<global::Android.Support.V4.View.ViewPager>(Resource.Id.pager);
            pager.Adapter = pageAdapter;

            TabLayout tabLayout = FindViewById<TabLayout>(Resource.Id.tabLayout);
            tabLayout.SetupWithViewPager(pager);

            SupportActionBar.Show();

            GetCheckLogin();
        }

        private async void GetCheckLogin()
        {
            if (!await Storage.InitializeLogin())
            {
                // Login invalid
                Analytics.TrackEvent("MainActivity_InvalidLogin");
                var suppress = AndroidUtils.ReturnToSignIn(this);
                return;
            }

            Analytics.TrackEvent("MainActivity_ValidLogin");

            UpdateTaskTypes();

            MapsInitializer.Initialize(this);
            MobileBarcodeScanner.Initialize(Application);

            global::Android.Net.Uri dataUri = base.Intent.Data;

            if (dataUri == null)
            {
                return;
            }

            string activityRef = dataUri.GetQueryParameter("code");
            if (!string.IsNullOrWhiteSpace(activityRef))
            {
                GetAndOpenActivity(activityRef);
            }
        }

        public async Task<ApplicationUser> GetCurrentUser()
        {
            DatabaseManager manager = await AndroidUtils.GetDbManager().ConfigureAwait(false);

            if (manager.CurrentUser == null)
            {
                // Something bad has happened, log out
                var suppress = AndroidUtils.ReturnToSignIn(this);
            }

            return manager.CurrentUser;
        }

        public async Task<List<FeedSection>> GetCachedContent(bool ownedOnly)
        {
            ApplicationUser currentUser = await GetCurrentUser().ConfigureAwait(false);

            string contentJsonCache = (ownedOnly)
                ? currentUser?.RemoteCreatedContentJson
                : currentUser?.CachedContentJson;

            try
            {
                if (string.IsNullOrWhiteSpace(contentJsonCache)) return new List<FeedSection>();

                return JsonConvert.DeserializeObject<List<FeedSection>>(contentJsonCache,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 12
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                RunOnUiThread(() => Toast.MakeText(this, Resource.String.errorCache, ToastLength.Long).Show());

                if (currentUser != null)
                {
                    currentUser.CachedContentJson = null;
                    (await AndroidUtils.GetDbManager().ConfigureAwait(false)).AddUser(currentUser);
                }

                return new List<FeedSection>();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.MainMenu, menu);
            return base.OnPrepareOptionsMenu(menu);
        }

        private async void GetScan()
        {
            MobileBarcodeScanner scanner = new MobileBarcodeScanner();
            ZXing.Result result = await scanner.Scan();

            if (result == null)
            {
                return;
            }

            Console.WriteLine("Scanned Barcode: " + result.Text);
            global::Android.Net.Uri uri = global::Android.Net.Uri.Parse(result.Text);

            if (uri == null)
            {
                return;
            }

            GetAndOpenActivity(uri.GetQueryParameter("code"));
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menuscan:
                    GetScan();
                    return true;

                case Resource.Id.menusearch:
                    StartSearch();
                    return true;

                case Resource.Id.menuuploads:
                    Intent uploadIntent = new Intent(this, typeof(UploadsActivity));
                    StartActivity(uploadIntent);
                    return true;

                case Resource.Id.menusettings:
                    Intent settingsIntent = new Intent(this, typeof(PreferencesActivity));
                    StartActivity(settingsIntent);
                    return true;

                case Resource.Id.menuhelp:
                    Intent browserIntent =
                        new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(
                            ConfidentialData.api + "GettingStarted"));
                    StartActivity(browserIntent);
                    return true;

                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        private void StartSearch()
        {
            global::Android.Support.V7.App.AlertDialog.Builder builder = new global::Android.Support.V7.App.AlertDialog.Builder(this);
            builder.SetTitle(Resource.String.searchAlertTitle);

            int px = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 16, Resources.DisplayMetrics);

            TextView message = new TextView(this);
            message.SetText(Resource.String.searchAlertMessage);
            EditText codeInput = new EditText(this);

            LinearLayout dialogLayout = new LinearLayout(this) { Orientation = Orientation.Vertical };
            dialogLayout.AddView(message);
            dialogLayout.AddView(codeInput);
            dialogLayout.SetPadding(px, px, px, px);

            builder.SetView(dialogLayout);
            builder.SetPositiveButton(Resource.String.MenuSearch, (a, b) => { GetAndOpenActivity(codeInput.Text); });
            builder.SetNeutralButton(Resource.String.dialog_cancel, (a, b) => { });
            builder.Show();
        }

        private async void GetAndOpenActivity(string code)
        {
            ProgressDialog dialog = new ProgressDialog(this);
            dialog.SetMessage(Resources.GetString(Resource.String.PleaseWait));
            dialog.Show();

            ServerResponse<LearningActivity> result = await ServerUtils.Get<LearningActivity>("/api/LearningActivities/GetWithCode?code=" + code).ConfigureAwait(false);

            RunOnUiThread(() =>
            {
                dialog.Dismiss();
                dialog.Dispose();
            });

            if (result == null)
            {
                var suppress = AndroidUtils.ReturnToSignIn(this);
                RunOnUiThread(() => Toast.MakeText(this, Resource.String.ForceSignOut, ToastLength.Long).Show());
                return;
            }

            if (result.Success)
            {
                await AndroidUtils.LaunchActivity(result.Data, this).ConfigureAwait(false);
            }
            else
            {
                // if token invalid, return to signin 
                if (ServerUtils.CheckNeedsLogin(result.StatusCode))
                {
                    var suppress = AndroidUtils.ReturnToSignIn(this);
                    return;
                }
                if (result.Message.StartsWith("404"))
                {
                    RunOnUiThread(() => Toast.MakeText(this, Resource.String.searchFail, ToastLength.Long).Show());
                }
                else
                {
                    RunOnUiThread(() => Toast.MakeText(this, Resource.String.ConnectionError, ToastLength.Long).Show());
                }
            }
        }

        // Update the TaskTypes available in the background
        public async Task UpdateTaskTypes()
        {
            DatabaseManager db = await AndroidUtils.GetDbManager().ConfigureAwait(false);

            List<TaskType> taskTypes = await ServerUtils.RefreshTaskTypes(db).ConfigureAwait(false);

            if (taskTypes == null)
            {
                var suppress = AndroidUtils.ReturnToSignIn(this);
                RunOnUiThread(() => Toast.MakeText(this, Resource.String.ForceSignOut, ToastLength.Long).Show());
                return;
            }

            if (!taskTypes.Any())
            {
                return;
            }

            db.AddTaskTypes(taskTypes);
        }
    }
}

