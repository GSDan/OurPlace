
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Android.Adapters;
using OurPlace.Common.LocalData;
using OurPlace.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Add an Activity", Theme = "@style/OurPlaceActionBar")]
    public class CreateChooseActivityActivity : AppCompatActivity
    {
        private RecyclerView recyclerView;
        private RecyclerView.LayoutManager layoutManager;
        private ActivityAdapter adapter;
        private DatabaseManager dbManager;
        private List<LearningActivity> activities;
        private List<LearningActivity> previouslySelected;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.CreateChooseTaskTypeActivity);

            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            previouslySelected = JsonConvert.DeserializeObject<List<LearningActivity>>(jsonData);

            using (TextView header = FindViewById<TextView>(Resource.Id.headerText))
            {
                header.SetText(Resource.String.createCollectionAddActivityHeader);
            }

            _ = GetUserActivities();
        }

        private async Task GetUserActivities()
        {
            if(dbManager == null)
            {
                dbManager = await Storage.GetDatabaseManager().ConfigureAwait(false);
            }

            activities = JsonConvert.DeserializeObject<List<LearningActivity>>(dbManager.CurrentUser.RemoteCreatedActivitiesJson) ?? new List<LearningActivity>();
            activities.RemoveAll(act => previouslySelected.Any((rhs) => rhs.Id == act.Id)); //remove previously selected activities
            activities = activities.OrderByDescending(act => act.CreatedAt).ToList();

            SetupAdaptors();

            // TODO enable refresh
        }

        private void SetupAdaptors()
        {
            adapter = new ActivityAdapter(this, activities);
            adapter.ItemClick += Adapter_ItemClick;

            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            recyclerView.SetAdapter(adapter);

            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);
        }

        private void Adapter_ItemClick(object sender, int position)
        {
            LearningActivity chosen = adapter.Data[position];

            using (Intent myIntent = new Intent(this, typeof(CreateCollectionOverviewActivity)))
            {
                myIntent.PutExtra("JSON", JsonConvert.SerializeObject(chosen));
                SetResult(Result.Ok, myIntent);
                base.Finish();
            }            
        }
    }
}