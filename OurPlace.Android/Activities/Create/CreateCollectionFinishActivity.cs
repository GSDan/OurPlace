using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
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
using Place = OurPlace.Common.Models.Place;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Final Touches", Theme = "@style/OurPlaceActionBar")]
    public class CreateCollectionFinishActivity : AppCompatActivity
    {
        private ActivityCollection collection;
        private CheckBox collectionPublic;
        private const int placePickerReq = 112;
        private List<Place> chosenPlaces;
        private bool editingSubmitted;
        private Random rand;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CreateFinishActivity);

            rand = new Random();

            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            editingSubmitted = Intent.GetBooleanExtra("EDITING_SUBMITTED", false);
            collection = JsonConvert.DeserializeObject<ActivityCollection>(jsonData, 
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            collectionPublic = FindViewById<CheckBox>(Resource.Id.checkboxPublic);
            collectionPublic.Text = string.Format(CultureInfo.InvariantCulture, collectionPublic.Text, "Collection");

            using(TextView publicBlurb = FindViewById<TextView>(Resource.Id.publicBlurb),
                addPlaceTitle = FindViewById<TextView>(Resource.Id.addPlaceTitle),
                addPlaceBlurb = FindViewById<TextView>(Resource.Id.addPlaceBlurb),
                reqNameBlurb = FindViewById<TextView>(Resource.Id.reqNameBlurb))
            {
                reqNameBlurb.Visibility = ViewStates.Gone;
                publicBlurb.Text = string.Format(CultureInfo.InvariantCulture, publicBlurb.Text, "Collection");
                addPlaceTitle.Text = string.Format(CultureInfo.InvariantCulture, addPlaceTitle.Text, "Collection");
                addPlaceBlurb.Text = string.Format(CultureInfo.InvariantCulture, addPlaceBlurb.Text, "Collection");
            }

            using(CheckBox reqName = FindViewById<CheckBox>(Resource.Id.checkboxReqName))
            {
                reqName.Visibility = ViewStates.Gone;
            }

            using (Button upload = FindViewById<Button>(Resource.Id.uploadBtn),
                choosePlace = FindViewById<Button>(Resource.Id.addPlaceBtn))
            {
                upload.Click += Upload_Click;
                choosePlace.Click += ChoosePlace_Click;
            }

            if (editingSubmitted)
            {
                collectionPublic.Checked = collection.IsPublic;
            }

            if (collection.Places == null) collection.Places = new List<Place>();

            foreach(LearningActivity act in collection.Activities)
            {
                if(act.Places != null)
                {
                    collection.Places.AddRange(act.Places);
                }
            }

            chosenPlaces = new List<Place>();

            foreach (Place place in collection.Places)
            {
                AddPlace(place, false);
            }
        }

        private void ChoosePlace_Click(object sender, EventArgs args)
        {
            try
            {
                using (var builder = new PlacePicker.IntentBuilder())
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

        private async void Upload_Click(object sender, EventArgs e)
        {
            collection.IsPublic = collectionPublic.Checked;
            collection.Places = chosenPlaces;

            collection.CollectionVersionNumber = (editingSubmitted) ? collection.CollectionVersionNumber + 1 : 0;

            _ = await Storage.PrepCreationForUpload(collection, editingSubmitted).ConfigureAwait(false);

            using (Intent intent = new Intent(this, typeof(UploadsActivity)))
            {
                StartActivity(intent);
                Finish();
            }
        }

        private void AddPlace(Place newPlace, bool showToast = true)
        {
            if (chosenPlaces.Exists((p) => p.GooglePlaceId == newPlace.GooglePlaceId))
            {
                if(showToast)
                {
                    Toast.MakeText(this, $"Already added '{newPlace.Name}'", ToastLength.Long).Show();
                }
                return;
            }

            View child = LayoutInflater.Inflate(Resource.Layout.CreateTaskMultipleChoiceEntry, null);
            child.FindViewById<TextView>(Resource.Id.option).Text = newPlace.Name;
            child.FindViewById<ImageButton>(Resource.Id.deleteButton).Click += DeletePlace;
            child.Id = rand.Next();

            chosenPlaces.Add(newPlace);

            using(LinearLayout choicesRoot = FindViewById<LinearLayout>(Resource.Id.placesRoot))
            {
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

            using(var choicesRoot = FindViewById<LinearLayout>(Resource.Id.placesRoot))
            {
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
            
        }
    }
}