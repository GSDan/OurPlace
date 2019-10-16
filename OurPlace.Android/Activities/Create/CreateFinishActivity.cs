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
using Android.Gms.Location.Places;
using Android.Gms.Location.Places.UI;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Common.LocalData;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Place = OurPlace.Common.Models.Place;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Final Touches", Theme = "@style/OurPlaceActionBar")]
    public class CreateFinishActivity : AppCompatActivity
    {
        private LearningActivity activity;
        private CheckBox activityPublic;
        private CheckBox reqUsername;
        private const int placePickerReq = 112;
        private List<Place> chosenPlaces;
        private LinearLayout choicesRoot;
        private Random rand;
        private bool editingSubmitted;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CreateFinishActivity);

            rand = new Random();

            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            editingSubmitted = Intent.GetBooleanExtra("EDITING_SUBMITTED", false);
            activity = JsonConvert.DeserializeObject<LearningActivity>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            FindViewById<Button>(Resource.Id.uploadBtn).Click += FinishClicked;
            FindViewById<Button>(Resource.Id.addPlaceBtn).Click += AddPlaceClicked;

            choicesRoot = FindViewById<LinearLayout>(Resource.Id.placesRoot);
            activityPublic = FindViewById<CheckBox>(Resource.Id.checkboxPublic);
            activityPublic.Text = string.Format(CultureInfo.InvariantCulture, activityPublic.Text, "Activity");
            reqUsername = FindViewById<CheckBox>(Resource.Id.checkboxReqName);

            using (TextView publicBlurb = FindViewById<TextView>(Resource.Id.publicBlurb),
                addPlaceTitle = FindViewById<TextView>(Resource.Id.addPlaceTitle),
                addPlaceBlurb = FindViewById<TextView>(Resource.Id.addPlaceBlurb))
            {
                publicBlurb.Text = string.Format(CultureInfo.InvariantCulture, publicBlurb.Text, "Activity");
                addPlaceTitle.Text = string.Format(CultureInfo.InvariantCulture, addPlaceTitle.Text, "Activity");
                addPlaceBlurb.Text = string.Format(CultureInfo.InvariantCulture, addPlaceBlurb.Text, "Activity");
            }

            chosenPlaces = new List<Place>();

            if (editingSubmitted)
            {
                activityPublic.Checked = activity.IsPublic;
                reqUsername.Checked = activity.RequireUsername;

                if (activity.Places != null)
                {
                    foreach (Place place in activity.Places)
                    {
                        AddPlace(place);
                    }
                }
            }
        }

        private void AddPlaceClicked(object sender, EventArgs args)
        {
            try
            {
                using(var builder = new PlacePicker.IntentBuilder())
                {
                    using (Intent intent = builder.Build(this))
                    {
                        StartActivityForResult(intent, placePickerReq);
                    }
                }
            }
            catch (Exception e)
            {
                Toast.MakeText(this, "Place err: " + e.Message, ToastLength.Long).Show();
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (requestCode != placePickerReq || resultCode != Result.Ok) return;

            IPlace place = PlaceAutocomplete.GetPlace(this, data);

            Place newPlace = new Place
            {
                GooglePlaceId = place.Id,
                Latitude = new decimal(place.LatLng.Latitude),
                Longitude = new decimal(place.LatLng.Longitude),
                Name = place.NameFormatted.ToString()
            };

            AddPlace(newPlace);
        }

        private void AddPlace(Place newPlace)
        {
            if (chosenPlaces.Exists((p) => p.GooglePlaceId == newPlace.GooglePlaceId))
            {
                Toast.MakeText(this, $"Already added '{newPlace.Name}'", ToastLength.Long).Show();
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

        private void FinishClicked(object sender, EventArgs e)
        {
            _ = SaveAndFinish();
        }

        private async Task SaveAndFinish()
        {
            activity.IsPublic = activityPublic.Checked;
            activity.RequireUsername = reqUsername.Checked;
            activity.Places = chosenPlaces;

            activity.ActivityVersionNumber = (editingSubmitted) ? activity.ActivityVersionNumber + 1 : 0;

            var uploadData = await Storage.PrepCreationForUpload(activity, editingSubmitted);

            using (Intent intent = new Intent(this, typeof(UploadsActivity)))
            {
                StartActivity(intent);
                Finish();
            }
        }
    }
}