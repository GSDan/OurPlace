using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Android.Adapters;
using OurPlace.Android.Fragments;
using OurPlace.Common.LocalData;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Edit Collection", Theme = "@style/OurPlaceActionBar", ParentActivity = typeof(MainActivity), LaunchMode = LaunchMode.SingleTask)]
    public class CreateCollectionOverviewActivity : AppCompatActivity
    {
        private ActivityCollection newCollection;
        private bool editingSubmitted;
        private ActivityCollectionAdapter adapter;
        private RecyclerView.LayoutManager layoutManager;
        private TextView fabPrompt;
        private DatabaseManager dbManager;
        private const int editCollectionIntent = 199;
        private const int addActivityIntent = 200;
        private const int viewLocIntent = 200;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.CreateCollectionActivity);

            editingSubmitted = Intent.GetBooleanExtra("EDITING_SUBMITTED", false);
            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            newCollection = JsonConvert.DeserializeObject<ActivityCollection>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            adapter = new ActivityCollectionAdapter(this, newCollection, SaveProgress);
            adapter.DeleteItemClick += Adapter_DeleteItemClick;
            adapter.EditCollectionClick += Adapter_EditCollectionClick;
            adapter.FinishClick += Adapter_FinishClick;
            adapter.OpenLocationClick += Adapter_OpenLocationClick;

            fabPrompt = FindViewById<TextView>(Resource.Id.fabPrompt);

            RecyclerView recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            recyclerView.SetAdapter(adapter);

            ItemTouchHelper.Callback callback = new DragHelper(adapter);
            ItemTouchHelper touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(recyclerView);

            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);

            using (FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.addActivityFab))
            {
                fab.Click += Fab_Click;
            }
        }

        protected override void OnResume()
        {
            SaveProgress();
            adapter?.NotifyDataSetChanged();
            base.OnResume();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.HelpOnlyMenu, menu);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item?.ItemId)
            {
                case Resource.Id.menuhelp:
                    using (global::Android.Support.V7.App.AlertDialog.Builder alert = new global::Android.Support.V7.App.AlertDialog.Builder(this))
                    {
                        alert.SetMessage(Resource.String.createCollectionHelp);
                        alert.SetPositiveButton(Resource.String.dialog_ok, (a, b) => { });
                        alert.Show();
                    }
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        private void Fab_Click(object sender, EventArgs e)
        {
            using (Intent intent = new Intent(this, typeof(CreateChooseActivityActivity)))
            {
                intent.PutExtra("JSON", JsonConvert.SerializeObject(adapter.Collection.Activities));
                StartActivityForResult(intent, addActivityIntent);
            }
        }

        private void Adapter_OpenLocationClick(object sender, int position)
        {
            position--;
            LearningActivity thisAct = adapter.Collection.Activities[position];
            Place thisPlace = thisAct.Places?.FirstOrDefault();

            if (thisPlace == null) return;

            global::Android.Net.Uri mapsIntentUri = global::Android.Net.Uri.Parse(
                    string.Format(CultureInfo.InvariantCulture,
                    "geo:{0},{1}?q={0},{1}(Target+Location)",
                    thisPlace.Latitude,
                    thisPlace.Longitude));

            using (Intent mapIntent = new Intent(Intent.ActionView, mapsIntentUri))
            {
                mapIntent.SetPackage("com.google.android.apps.maps");
                StartActivity(mapIntent);
            }
        }

        private void Adapter_FinishClick(object sender, int e)
        {
            using (Intent intent = new Intent(this, typeof(CreateCollectionFinishActivity)))
            {
                StringBuilder sb = new StringBuilder();

                foreach (LearningActivity act in adapter.Collection.Activities)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", (sb.Length > 0) ? "," : "", act.Id);
                }

                adapter.Collection.ActivityOrder = sb.ToString();

                intent.PutExtra("JSON", JsonConvert.SerializeObject(adapter.Collection));
                intent.PutExtra("EDITING_SUBMITTED", editingSubmitted);
                StartActivity(intent);
            } 
        }

        private void Adapter_EditCollectionClick(object sender, int e)
        {
            // Edit the collection's basic details
            Intent intent = new Intent(this, typeof(CreateCollectionActivity));
            newCollection = adapter.Collection;
            intent.PutExtra("JSON", JsonConvert.SerializeObject(newCollection));
            StartActivityForResult(intent, editCollectionIntent);
            intent.Dispose();
        }

        private void Adapter_DeleteItemClick(object sender, int position)
        {
            using (var diag = new global::Android.Support.V7.App.AlertDialog.Builder(this))
            {
                diag.SetTitle(Resource.String.deleteTitle)
                .SetMessage(Resource.String.deleteMessage)
                .SetNegativeButton(Resource.String.dialog_cancel, (a, e) => { })
                .SetPositiveButton(Resource.String.DeleteBtn, (a, b) =>
                {
                    position--;
                    adapter.Collection.Activities.RemoveAt(position);
                    adapter.NotifyDataSetChanged();
                    SaveProgress();
                })
                .Show();
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] global::Android.App.Result resultCode, Intent data)
        {
            if (resultCode != Result.Ok || data == null) return;

            switch (requestCode)
            {
                case editCollectionIntent:
                    {
                        ActivityCollection returned = JsonConvert.DeserializeObject<ActivityCollection>(data.GetStringExtra("JSON"));
                        if (returned != null)
                        {
                            newCollection = returned;
                            adapter.UpdateActivity(returned);
                        }
                        break;
                    }
                case addActivityIntent:
                    {
                        LearningActivity added = JsonConvert.DeserializeObject<LearningActivity>(data.GetStringExtra("JSON"));
                        adapter.Collection.Activities.Add(added);
                        adapter.NotifyDataSetChanged();
                        break;
                    }
            }
        }

        public async void SaveProgress()
        {
            newCollection = adapter.Collection;

            // Hide the prompt if the user has added at least 2 activities
            fabPrompt.Visibility =
                (newCollection.Activities != null && newCollection.Activities.Count >= 1)
                    ? ViewStates.Gone : ViewStates.Visible;

            // Don't save changes to uploaded activities until we're ready to submit
            if (editingSubmitted) return;

            if (dbManager == null)
            {
                dbManager = await Storage.GetDatabaseManager();
            }

            // Add/update this new activity in the user's inprogress cache
            string cacheJson = dbManager.CurrentUser.LocalCreatedCollectionsJson;
            List<ActivityCollection> inProgress = (string.IsNullOrWhiteSpace(cacheJson)) ?
                new List<ActivityCollection>() :
                JsonConvert.DeserializeObject<List<ActivityCollection>>(cacheJson);

            int existingInd = inProgress.FindIndex((la) => la.Id == newCollection.Id);

            if (existingInd == -1)
            {
                inProgress.Insert(0, newCollection);
            }
            else
            {
                inProgress.RemoveAt(existingInd);
                inProgress.Insert(0, newCollection);
            }

            dbManager.CurrentUser.LocalCreatedCollectionsJson = JsonConvert.SerializeObject(inProgress);
            dbManager.AddUser(dbManager.CurrentUser);
            MainMyCreationsFragment.ForceRefresh = true;
        }
    }
}