using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Android.Activities.Abstracts;
using OurPlace.Android.Adapters;
using OurPlace.Common;
using OurPlace.Common.Models;

namespace OurPlace.Android.Activities
{
    [Activity(Label = "OurPlace", ParentActivity = typeof(MainActivity), LaunchMode = LaunchMode.SingleTop)]
    public class CollectionActivityListActivity : HeaderImageActivity
    {
        private ActivityCollectionAdapter adapter;
        private ActivityCollection collection;
        private const int PermReqId = 111;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.RecyclerViewActivity);

            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            collection = JsonConvert.DeserializeObject<ActivityCollection>(jsonData,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            adapter = new ActivityCollectionAdapter(this, collection, null, false);
            adapter.OpenItemClick += Adapter_OpenItemClick;
            adapter.OpenLocationClick += Adapter_OpenLocationClick;

            using (var toolbar = FindViewById<global::Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar))
            {
                SetSupportActionBar(toolbar);
                SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            }

            LoadHeaderImage(collection.ImageUrl);

            using (var recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView))
            {
                recyclerView.SetAdapter(adapter);
                using (var layoutManager = new LinearLayoutManager(this))
                {
                    recyclerView.SetLayoutManager(layoutManager);
                }
            }

            _ = DownloadActivities();
        }

        private void Adapter_OpenLocationClick(object sender, int position)
        {
            LearningActivity thisAct = adapter.Collection.Activities[position];
            Place thisPlace = thisAct.Places?.FirstOrDefault();

            if (thisPlace == null) return;

            using (var lastReqIntent = new Intent(this, typeof(LocationHuntActivity)))
            {
                lastReqIntent.PutExtra("Target", JsonConvert.SerializeObject(
                    new LocationHuntLocation((double)thisPlace.Latitude, (double)thisPlace.Longitude, 15.0f, true)));

                AndroidUtils.CallWithPermission(new string[] { global::Android.Manifest.Permission.AccessFineLocation },
                    new string[] { base.Resources.GetString(Resource.String.permissionLocationTitle) },
                    new string[] { base.Resources.GetString(Resource.String.permissionLocationExplanation) },
                    lastReqIntent, thisAct.Id, PermReqId, this);
            }
        }

        private async void Adapter_OpenItemClick(object sender, int pos)
        {
            LearningActivity act = adapter.Collection.Activities[pos];
            await AndroidUtils.LaunchActivity(act, this).ConfigureAwait(false);
        }
        public override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            if (collection != null)
            {
                using(var toolbar = FindViewById<global::Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar))
                {
                    toolbar.Title = collection.Name;
                }
            }
        }

        private async Task DownloadActivities()
        {
            foreach(LearningActivity act in collection.Activities)
            {
                try
                {
                    bool success = await AndroidUtils.PrepActivityFiles(this, act).ConfigureAwait(false);
                    if (!success)
                    {
                        RunOnUiThread(() => Toast.MakeText(this, $"{GetString(Resource.String.ConnectionError)}", ToastLength.Long).Show());
                        return;
                    }
                }
                catch (Exception e)
                {
                    RunOnUiThread(() => Toast.MakeText(this, $"{GetString(Resource.String.ErrorTitle)}: {e.Message}", ToastLength.Long).Show());
                    return;
                }
            }
            
        }
    }
}