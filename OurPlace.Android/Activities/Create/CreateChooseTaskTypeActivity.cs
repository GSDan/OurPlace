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
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Newtonsoft.Json;
using OurPlace.Android.Adapters;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Android.Widget;
using OurPlace.Common;
using OurPlace.Common.LocalData;
using static OurPlace.Common.LocalData.Storage;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "New Task", Theme = "@style/OurPlaceActionBar")]
    public class CreateChooseTaskTypeActivity : AppCompatActivity
    {
        private RecyclerView recyclerView;
        private RecyclerView.LayoutManager layoutManager;
        private TaskTypeAdapter adapter;
        private List<TaskType> taskTypes;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.CreateChooseTaskTypeActivity);

            GetTaskTypes();
        }

        private async void GetTaskTypes()
        {
            taskTypes = (await GetDatabaseManager()).GetTaskTypes().ToList();

            // If the database is no good try to pull TaskTypes from the server
            if (taskTypes == null || taskTypes.Count == 0)
            {
                ProgressDialog progDialog = new ProgressDialog(this);
                progDialog.SetMessage(Resources.GetString(Resource.String.Connecting));
                progDialog.Show();

                DatabaseManager dbManager = await GetDatabaseManager();
                List<TaskType> loadedTypes = await ServerUtils.RefreshTaskTypes(dbManager);

                progDialog.Dismiss();

                if (loadedTypes == null)
                {
                    // can't get a valid token, return to sign in to re-authorise
                    var suppress = AndroidUtils.ReturnToSignIn(this);
                    Toast.MakeText(this, Resource.String.ForceSignOut, ToastLength.Long).Show();
                    return;
                }

                if (loadedTypes.Count == 0)
                {
                    new global::Android.Support.V7.App.AlertDialog.Builder(this)
                        .SetTitle(Resource.String.ErrorTitle)
                        .SetMessage(Resource.String.ConnectionError)
                        .SetCancelable(false)
                        .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { base.Finish(); })
                        .Show();
                    return;
                }

                dbManager.AddTaskTypes(loadedTypes);

                taskTypes = loadedTypes;
                SetupAdaptors();

            }

            SetupAdaptors();
        }

        private void SetupAdaptors()
        {
            adapter = new TaskTypeAdapter(this, taskTypes);
            adapter.ItemClick += Adapter_ItemClick;

            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            recyclerView.SetAdapter(adapter);

            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);
        }

        private void Adapter_ItemClick(object sender, int position)
        {
            TaskType chosenType = adapter.Data[position];
            Type activityType = AndroidUtils.GetTaskCreationActivityType(chosenType.IdName);

            Intent myIntent = new Intent(this, activityType);
            myIntent.PutExtra("JSON", JsonConvert.SerializeObject(chosenType));
            myIntent.PutExtra("PARENT", Intent.GetStringExtra("PARENT")); // null if not a child task
            StartActivityForResult(myIntent, 200);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] global::Android.App.Result resultCode, Intent data)
        {
            // If successful return pass the json data back to the parent activity
            if (resultCode == global::Android.App.Result.Ok)
            {
                Intent myIntent = new Intent(this, typeof(CreateManageTasksActivity));
                myIntent.PutExtra("JSON", data.GetStringExtra("JSON"));
                SetResult(global::Android.App.Result.Ok, myIntent);
                base.Finish();
            }
        }
    }
}