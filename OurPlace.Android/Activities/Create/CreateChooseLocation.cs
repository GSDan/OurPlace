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
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Location;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Android.Adapters;
using OurPlace.Common;
using OurPlace.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Choose Location", Theme = "@style/OurPlaceActionBar")]
    public class CreateChooseLocation : AppCompatActivity, GoogleApiClient.IConnectionCallbacks, GoogleApiClient.IOnConnectionFailedListener
    {
        private List<Place> previouslyChosen;
        private Map_Location targetLoc;
        private GoogleApiClient googleApiClient;
        private ProgressDialog dialog;
        private PlacesAdapter adapter;
        private RecyclerView recyclerView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CreateChooseLocationActivity);

            string chosenData = Intent.GetStringExtra("CHOSEN") ?? "";
            previouslyChosen = !string.IsNullOrWhiteSpace(chosenData) ?
                JsonConvert.DeserializeObject<List<Place>>(chosenData) :
                new List<Place>();

            dialog = new ProgressDialog(this);
            dialog.SetMessage(Resources.GetString(Resource.String.Connecting));
            dialog.Show();

            string targetData = Intent.GetStringExtra("TARGET") ?? "";
            if (!string.IsNullOrWhiteSpace(targetData))
            {
                targetLoc = JsonConvert.DeserializeObject<Map_Location>(targetData);
                LoadData();
            }
            else
            {
                if (AndroidUtils.IsGooglePlayServicesInstalled(this) && googleApiClient == null)
                {
                    googleApiClient = new GoogleApiClient.Builder(this)
                        .AddConnectionCallbacks(this)
                        .AddOnConnectionFailedListener(this)
                        .AddApi(LocationServices.API)
                        .Build();
                }
            }
        }

        private async void LoadData()
        {
            ServerResponse<GMapsResultColl> resp =
                    await ServerUtils.Get<GMapsResultColl>(
                        string.Format("https://maps.googleapis.com/maps/api/place/nearbysearch/json?key={0}&location={1},{2}&radius=4000&type=park",
                        Resources.GetString(Resource.String.MapsApiKey), targetLoc.Lat, targetLoc.Long));

            dialog.Dismiss();

            if (resp == null)
            {
                var suppress = AndroidUtils.ReturnToSignIn(this);
                Toast.MakeText(this, Resource.String.ForceSignOut, ToastLength.Long).Show();
                return;
            }

            if (!resp.Success)
            {
                Toast.MakeText(this, Resource.String.ConnectionError, ToastLength.Long).Show();
                return;
            }

            // Don't list places that have already been added
            List<GooglePlaceResult> final = new List<GooglePlaceResult>();
            foreach (GooglePlaceResult res in resp.Data.results)
            {
                if (previouslyChosen.All(p => p.GooglePlaceId != res.place_id))
                {
                    final.Add(res);
                }
            }

            adapter = new PlacesAdapter(this, final);
            adapter.ItemClick += Adapter_ItemClick; ;

            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            recyclerView.SetAdapter(adapter);

            LinearLayoutManager layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);
        }

        protected override void OnStart()
        {
            googleApiClient?.Connect();
            base.OnStart();
        }

        protected override void OnStop()
        {
            googleApiClient?.Disconnect();
            base.OnStop();
        }

        public void OnConnected(Bundle connectionHint)
        {
            global::Android.Locations.Location lastKnown = LocationServices.FusedLocationApi.GetLastLocation(googleApiClient);
            if (lastKnown != null)
            {
                targetLoc = new Map_Location(lastKnown.Latitude, lastKnown.Longitude, 15);
                LoadData();
            }
        }

        private void Adapter_ItemClick(object sender, int e)
        {
            GooglePlaceResult res = adapter.Data[e];
            string json = JsonConvert.SerializeObject(
                    new Place
                    {
                        GooglePlaceId = res.place_id,
                        Latitude = new decimal(res.geometry.location.lat),
                        Longitude = new decimal(res.geometry.location.lng),
                        Name = res.name
                    }
                );

            Intent myIntent = new Intent(this, typeof(CreateFinishActivity));
            myIntent.PutExtra("JSON", json);
            SetResult(Result.Ok, myIntent);
            Finish();
        }

        public void OnConnectionSuspended(int cause)
        {
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
        }
    }
}