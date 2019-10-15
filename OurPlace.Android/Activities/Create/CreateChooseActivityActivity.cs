
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
using Android.Views;
using OurPlace.Common;
using ZXing.Mobile;
using Android.Util;

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
            if (dbManager == null)
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
            ActivityChosen(adapter.Data[position]);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.CreateCollectionAddActivityMenu, menu);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item?.ItemId)
            {
                case Resource.Id.menuscan:
                    GetScan();
                    return true;

                case Resource.Id.menusearch:
                    StartSearch();
                    return true;

                case Resource.Id.menuhelp:
                    ShowHelp();
                    return true;

                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        private async void GetScan()
        {
            MobileBarcodeScanner scanner = new MobileBarcodeScanner();
            ZXing.Result result = await scanner.Scan().ConfigureAwait(false);

            if (result == null)
            {
                return;
            }

            global::Android.Net.Uri uri = global::Android.Net.Uri.Parse(result.Text);

            if (uri == null)
            {
                return;
            }

            GetAndReturnWithActivity(uri.GetQueryParameter("code"));
        }
        private void StartSearch()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            EditText codeInput = new EditText(this);
#pragma warning restore CA2000 // Dispose objects before losing scope

            using (global::Android.Support.V7.App.AlertDialog.Builder builder = new global::Android.Support.V7.App.AlertDialog.Builder(this))
            using (TextView message = new TextView(this))
            using (LinearLayout dialogLayout = new LinearLayout(this) { Orientation = Orientation.Vertical })
            {
                builder.SetTitle(Resource.String.searchAlertTitle);
                int px = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 16, Resources.DisplayMetrics);
                message.SetText(Resource.String.searchAlertMessage);

                dialogLayout.AddView(message);
                dialogLayout.AddView(codeInput);
                dialogLayout.SetPadding(px, px, px, px);

                builder.SetView(dialogLayout);
                builder.SetPositiveButton(Resource.String.MenuSearch, (a, b) => { GetAndReturnWithActivity(codeInput.Text); codeInput.Dispose(); });
                builder.SetNeutralButton(Resource.String.dialog_cancel, (a, b) => { });
                builder.Show();
            }
        }

        private void ShowHelp()
        {
            using (global::Android.Support.V7.App.AlertDialog.Builder alert = new global::Android.Support.V7.App.AlertDialog.Builder(this))
            {
                alert.SetTitle(Resource.String.MenuHelp);
                alert.SetMessage(Resource.String.createCollectionAddActivityHelp);
                alert.SetPositiveButton(Resource.String.dialog_ok, (a, b) => { });
                alert.Show();
            }
        }

        private async void GetAndReturnWithActivity(string code)
        {
            ProgressDialog dialog = new ProgressDialog(this);
            dialog.SetMessage(Resources.GetString(Resource.String.PleaseWait));
            dialog.Show();

            ServerResponse<LearningActivity> result =
                await ServerUtils.Get<LearningActivity>("/api/LearningActivities/GetWithCode?code=" + code).ConfigureAwait(false);

            dialog.Dismiss();

            if (result == null)
            {
                _ = AndroidUtils.ReturnToSignIn(this);
                Toast.MakeText(this, Resource.String.ForceSignOut, ToastLength.Long).Show();
                return;
            }

            if (result.Success)
            {
                ActivityChosen(result.Data);
            }
            else
            {
                // if token invalid, return to signin 
                if (ServerUtils.CheckNeedsLogin(result.StatusCode))
                {
                    _ = AndroidUtils.ReturnToSignIn(this);
                    return;
                }

                if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Toast.MakeText(this, Resource.String.searchFail, ToastLength.Long).Show();
                }
                else
                {
                    Toast.MakeText(this, Resource.String.ConnectionError, ToastLength.Long).Show();
                }
            }
        }

        private void ActivityChosen(LearningActivity chosen)
        {
            if (chosen == null) return;

            using (Intent myIntent = new Intent(this, typeof(CreateCollectionOverviewActivity)))
            {
                myIntent.PutExtra("JSON", JsonConvert.SerializeObject(chosen));
                SetResult(Result.Ok, myIntent);
                base.Finish();
            }
        }
    }
}