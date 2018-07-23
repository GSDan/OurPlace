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
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Text.Method;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using OurPlace.Common.LocalData;
using Android.Gms.Location;
using Android.Gms.Location.Places.UI;
using Android.Gms.Common;
using Android.Gms.Location.Places;
using System.Threading.Tasks;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Final Touches", Theme = "@style/OurPlaceActionBar")]
    public class CreateFinishActivity : AppCompatActivity
    {
        private LearningActivity activity;
        private CheckBox activityPublic;
        private CheckBox reqUsername;
        private const int placePickerReq = 112;
        private List<Common.Models.Place> chosenPlaces;
        private LinearLayout choicesRoot;
        private Random rand;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CreateFinishActivity);

            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            activity = JsonConvert.DeserializeObject<LearningActivity>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            FindViewById<Button>(Resource.Id.uploadBtn).Click += FinishClicked;
            FindViewById<Button>(Resource.Id.addPlaceBtn).Click += AddPlaceClicked;

            choicesRoot = FindViewById<LinearLayout>(Resource.Id.placesRoot);
            activityPublic = FindViewById<CheckBox>(Resource.Id.checkboxPublic);
            reqUsername = FindViewById<CheckBox>(Resource.Id.checkboxReqName);
            chosenPlaces = new List<Common.Models.Place>();
            rand = new Random();
        }

        private void AddPlaceClicked(object sender, System.EventArgs args)
        {
            try
            {
                //Intent intent = new PlaceAutocomplete.IntentBuilder(PlaceAutocomplete.ModeFullscreen).Build(this);

                Intent intent = new PlacePicker.IntentBuilder().Build(this);
                StartActivityForResult(intent, placePickerReq);
            }
            catch (GooglePlayServicesRepairableException e)
            {
                Toast.MakeText(this, "Place err: " + e.Message, ToastLength.Long).Show();
            }
            catch (GooglePlayServicesNotAvailableException e)
            {
                Toast.MakeText(this, "Place err: " + e.Message, ToastLength.Long).Show();
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (requestCode == placePickerReq && resultCode == Result.Ok)
            {
                IPlace place = PlaceAutocomplete.GetPlace(this, data);

                Common.Models.Place newPlace = new Common.Models.Place
                {
                    GooglePlaceId = place.Id,
                    Latitude = new decimal(place.LatLng.Latitude),
                    Longitude = new decimal(place.LatLng.Longitude),
                    Name = place.NameFormatted.ToString()
                };

                if (chosenPlaces.Exists((p) => p.GooglePlaceId == place.Id))
                {
                    Toast.MakeText(this, string.Format("Already added '{0}'", newPlace.Name), ToastLength.Long).Show();
                    return;
                }

                View child = LayoutInflater.Inflate(Resource.Layout.CreateTaskMultipleChoiceEntry, null);
                child.FindViewById<TextView>(Resource.Id.option).Text = newPlace.Name;
                child.FindViewById<ImageButton>(Resource.Id.deleteButton).Click += DeletePlace;
                child.Id = rand.Next();

                chosenPlaces.Add(newPlace);
                choicesRoot.AddView(child);
                choicesRoot.Visibility = ViewStates.Visible;
            }
        }

        private void DeletePlace(object sender, EventArgs e)
        {
            ViewGroup parent = (ViewGroup)((ImageButton)sender).Parent;

            for (int i = 0; i < parent.ChildCount; i++)
            {
                View child = parent.GetChildAt(i);
                if (child.GetType() == typeof(AppCompatTextView))
                {
                    chosenPlaces.RemoveAll(p => p.Name == ((TextView)child).Text);
                    break;
                }
            }

            ViewGroup topParent = (ViewGroup)parent.Parent.Parent;

            for (int i = 0; i < choicesRoot.ChildCount; i++)
            {
                View child = choicesRoot.GetChildAt(i);
                if (child.Id == topParent.Id)
                {
                    choicesRoot.RemoveViewAt(i);
                    break;
                }
            }

            if (choicesRoot.ChildCount <= 1)
            {
                choicesRoot.Visibility = ViewStates.Gone;
            }
        }

        private void FinishClicked(object sender, System.EventArgs e)
        {
            var suppress = SaveAndFinish();
        }

        private async Task SaveAndFinish()
        {
            activity.IsPublic = activityPublic.Checked;
            activity.RequireUsername = reqUsername.Checked;
            activity.Places = chosenPlaces;

            var uploadData = await Storage.PrepCreatedActivityForUpload(activity);

            Intent intent = new Intent(this, typeof(UploadsActivity));
            StartActivity(intent);
            Finish();
        }
    }
}