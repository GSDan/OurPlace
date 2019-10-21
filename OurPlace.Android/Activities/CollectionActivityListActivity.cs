using System;
using System.Collections.Generic;
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
using OurPlace.Common.Models;

namespace OurPlace.Android.Activities
{
    [Activity(Label = "OurPlace", ParentActivity = typeof(MainActivity), LaunchMode = LaunchMode.SingleTop)]
    public class CollectionActivityListActivity : HeaderImageActivity
    {
        private ActivityCollectionAdapter adapter;
        private ActivityCollection collection;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.RecyclerViewActivity);

            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            collection = JsonConvert.DeserializeObject<ActivityCollection>(jsonData,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            adapter = new ActivityCollectionAdapter(this, collection, null);

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