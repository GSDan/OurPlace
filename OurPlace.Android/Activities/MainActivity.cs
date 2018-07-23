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
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Android.Activities.Create;
using OurPlace.Android.Adapters;
using OurPlace.Android.Fragments;
using OurPlace.Common;
using OurPlace.Common.Models;
using System;
using System.Linq;
using ZXing.Mobile;

namespace OurPlace.Android.Activities
{
    [Activity(Label = "OurPlace", Theme = "@style/OurPlaceActionBar", Icon = "@mipmap/ic_launcher")]
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
            if (!await Common.LocalData.Storage.InitializeLogin())
            {
                // Login invalid
                var suppress = AndroidUtils.ReturnToSignIn(this);
                return;
            }

            UpdateTaskTypes();

            MapsInitializer.Initialize(this);
            MobileBarcodeScanner.Initialize(Application);

            global::Android.Net.Uri dataUri = base.Intent.Data;

            if (dataUri != null)
            {
                string activityRef = dataUri.GetQueryParameter("code");
                if (!string.IsNullOrWhiteSpace(activityRef))
                {
                    GetAndOpenActivity(activityRef);
                }
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.MainMenu, menu);
            return base.OnPrepareOptionsMenu(menu);
        }

        private async void GetScan()
        {
            MobileBarcodeScanner scanner = new ZXing.Mobile.MobileBarcodeScanner();
            ZXing.Result result = await scanner.Scan();

            if (result == null) return;

            Console.WriteLine("Scanned Barcode: " + result.Text);
            global::Android.Net.Uri uri = global::Android.Net.Uri.Parse(result.Text);

            if (uri == null) return;

            GetAndOpenActivity(uri.GetQueryParameter("code"));
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.menuscan)
            {
                GetScan();
                return true;
            }
            if (item.ItemId == Resource.Id.menusearch)
            {
                StartSearch();
                return true;
            }
            if (item.ItemId == Resource.Id.menuuploads)
            {
                Intent intent = new Intent(this, typeof(UploadsActivity));
                StartActivity(intent);
                return true;
            }
            if (item.ItemId == Resource.Id.menuhelp)
            {
                Intent browserIntent =
                        new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(
                            ConfidentialData.api + "GettingStarted"));
                StartActivity(browserIntent);
                return true;
            }
            if (item.ItemId == Resource.Id.menulogout)
            {
                LogOut();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void StartSearch()
        {
            global::Android.Support.V7.App.AlertDialog.Builder builder = new global::Android.Support.V7.App.AlertDialog.Builder(this);
            builder.SetTitle(Resource.String.searchAlertTitle);

            int px = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 16, Resources.DisplayMetrics);

            TextView message = new TextView(this);
            message.SetText(Resource.String.searchAlertMessage);
            EditText codeInput = new EditText(this);

            LinearLayout dialogLayout = new LinearLayout(this);
            dialogLayout.Orientation = Orientation.Vertical;
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

            Common.ServerResponse<LearningActivity> result =
                await Common.ServerUtils.Get<LearningActivity>("/api/LearningActivities/GetWithCode?code=" + code);

            dialog.Dismiss();

            if (result == null)
            {
                var suppress = AndroidUtils.ReturnToSignIn(this);
                Toast.MakeText(this, Resource.String.ForceSignOut, ToastLength.Long).Show();
                return;
            }

            if (result.Success)
            {
                LaunchActivity(result.Data);
            }
            else
            {
                // if token invalid, return to signin 
                if (Common.ServerUtils.CheckNeedsLogin(result.StatusCode))
                {
                    var suppress = AndroidUtils.ReturnToSignIn(this);
                    return;
                }

                if (result.Message.StartsWith("404"))
                {
                    Toast.MakeText(this, Resource.String.searchFail, ToastLength.Long).Show();
                }
                else
                {
                    Toast.MakeText(this, Resource.String.ConnectionError, ToastLength.Long).Show();
                }
            }
        }

        private void LogOut()
        {
            new global::Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(Resource.String.MenuLogout)
                .SetMessage(Resource.String.LogoutConfirm)
                .SetPositiveButton(Resource.String.MenuLogout, (a, b) =>
                {
                    var suppress = AndroidUtils.ReturnToSignIn(this);
                })
                .SetNegativeButton(Resource.String.dialog_cancel, (a, b) => { })
                .Show();
        }

        // Update the TaskTypes available in the background
        public async void UpdateTaskTypes()
        {
            Common.ServerResponse<TaskType[]> response = await Common.ServerUtils.GetTaskTypes();

            if (response == null)
            {
                var suppress = AndroidUtils.ReturnToSignIn(this);
                Toast.MakeText(this, Resource.String.ForceSignOut, ToastLength.Long).Show();
                return;
            }

            if (response.Success)
            {
                (await Common.LocalData.Storage.GetDatabaseManager()).AddTaskTypes(response.Data);
            }
        }

        public async void LaunchActivity(LearningActivity activity)
        {
            int thisVersion = PackageManager.GetPackageInfo(PackageName, 0).VersionCode;

            if (activity.AppVersionNumber > thisVersion)
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.updateTitle)
                    .SetMessage(Resource.String.updateMessage)
                    .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                    .Show();
                return;
            }

            Intent performActivityIntent = new Intent(this, typeof(ActTaskListActivity));

            activity.LearningTasks = activity.LearningTasks.OrderBy(t => t.Order).ToList();

            // Save this activity to the database for showing in the 'recent' feed section
            (await Common.LocalData.Storage.GetDatabaseManager()).AddActivity(activity);

            string json = JsonConvert.SerializeObject(activity, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                MaxDepth = 5
            });
            performActivityIntent.PutExtra("JSON", json);
            MainLandingFragment.ForceRefresh = true;
            StartActivity(performActivityIntent);
        }
    }
}

