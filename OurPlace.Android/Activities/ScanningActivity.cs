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
using Android.Support.V7.App;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Android.Fragments;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using ZXing.Mobile;
using ZXing.Net.Mobile.Android;

namespace OurPlace.Android.Activities
{
    [Activity(Theme = "@style/OurPlaceActionBar", ScreenOrientation = ScreenOrientation.Portrait)]
    public class ScanningActivity : AppCompatActivity
    {
        private LearningTask learningTask;
        private ZXingScannerFragment scanFragment;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            string thisJsonData = Intent.GetStringExtra("JSON") ?? "";
            learningTask = JsonConvert.DeserializeObject<LearningTask>(thisJsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            SetContentView(Resource.Layout.ScanningActivity);

            SupportActionBar.Title = GetString(Resource.String.scanningActivity_title);
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (scanFragment == null)
            {
                scanFragment = new CustomScannerFragment
                {
                    BottomText = learningTask.Description
                };
                SupportFragmentManager.BeginTransaction()
                    .Replace(Resource.Id.fragment_container, scanFragment)
                    .Commit();
            }

            bool needsPermission = PermissionsHandler.NeedsPermissionRequest(this);

            if(needsPermission)
            {
                PermissionsHandler.RequestPermissionsAsync(this);
            }
            else
            {
                scanFragment.StartScanning(OnScanResult, GetOptions());
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public MobileBarcodeScanningOptions GetOptions()
        {
            return new MobileBarcodeScanningOptions
            {
                PossibleFormats = new List<ZXing.BarcodeFormat>
                {
                    ZXing.BarcodeFormat.QR_CODE
                }
            };
        }

        public void OnScanResult(ZXing.Result res)
        {
            if (res == null || string.IsNullOrEmpty(res.Text))
            {
                Toast.MakeText(this, Resource.String.scanningActivity_cancelled, ToastLength.Short).Show();
                return;
            }

            if (res.Text == Common.ServerUtils.GetTaskQRCodeData(learningTask.Id))
            {
                RunOnUiThread(() => Toast.MakeText(this, Resource.String.scanningActivity_success, ToastLength.Short).Show());
                ReturnSuccess();
            }

            RunOnUiThread(() => Toast.MakeText(this, Resource.String.scanningActivity_wrong, ToastLength.Short).Show());
        }

        protected override void OnPause()
        {
            scanFragment?.StopScanning();

            base.OnPause();
        }

        public void ReturnSuccess()
        {
            Intent myIntent = new Intent(this, typeof(ActTaskListActivity));
            myIntent.PutExtra("TASK_ID", learningTask.Id);
            myIntent.PutExtra("COMPLETE", true);
            SetResult(Result.Ok, myIntent);
            Finish();
        }
    }
}