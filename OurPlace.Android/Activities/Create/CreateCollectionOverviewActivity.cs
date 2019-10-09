using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using Newtonsoft.Json;
using OurPlace.Android.Adapters;
using OurPlace.Common.Models;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Edit Collection", Theme = "@style/OurPlaceActionBar", ParentActivity = typeof(MainActivity), LaunchMode = LaunchMode.SingleTask)]
    public class CreateCollectionOverviewActivity : Activity
    {
        private ActivityCollection newCollection;
        private bool editingSubmitted;
        private ActivityCollectionAdapter adapter;
        private RecyclerView recyclerView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.CreateCollectionActivity);

            editingSubmitted = Intent.GetBooleanExtra("EDITING_SUBMITTED", false);
            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            newCollection = JsonConvert.DeserializeObject<ActivityCollection>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            adapter = new ActivityCollectionAdapter(this, newCollection, SaveProgress);
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            recyclerView.SetAdapter(adapter);


        }

        public async void SaveProgress()
        {
        }
    }
}