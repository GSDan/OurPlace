using Android.App;
using Android.Content.PM;
using Android.OS;
using Newtonsoft.Json;
using OurPlace.Common.Models;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Edit Collection", Theme = "@style/OurPlaceActionBar", ParentActivity = typeof(MainActivity), LaunchMode = LaunchMode.SingleTask)]
    public class CreateCollectionActivity : Activity
    {
        private ActivityCollection newCollection;
        private bool editingSubmitted;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.CreateCollectionActivity);

            editingSubmitted = Intent.GetBooleanExtra("EDITING_SUBMITTED", false);
            string jsonData = Intent.GetStringExtra("JSON") ?? "";

            newCollection = JsonConvert.DeserializeObject<ActivityCollection>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

        }
    }
}