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
using OurPlace.Common;
using OurPlace.Common.LocalData;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static OurPlace.Common.LocalData.Storage;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Edit Activity", Theme = "@style/OurPlaceActionBar", ParentActivity = typeof(MainActivity), LaunchMode = LaunchMode.SingleTask)]
    public class CreateActivityOverviewActivity : AppCompatActivity
    {
        private LearningActivity newActivity;
        private bool editingSubmitted;
        private RecyclerView.LayoutManager layoutManager;
        private CreatedTasksAdapter adapter;
        private TextView fabPrompt;
        private DatabaseManager dbManager;
        private const int EditTaskIntent = 198;
        private const int EditActivityIntent = 199;
        private const int AddTaskIntent = 200;
        private const int ManageChildrenIntent = 201;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.CreateManageTasksActivity);

            editingSubmitted = Intent.GetBooleanExtra("EDITING_SUBMITTED", false);
            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            newActivity = JsonConvert.DeserializeObject<LearningActivity>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            adapter = new CreatedTasksAdapter(this, newActivity, editingSubmitted, SaveProgress);
            adapter.EditActivityClick += Adapter_EditActivityClick;
            adapter.FinishClick += Adapter_FinishClick;
            adapter.EditItemClick += Adapter_EditItemClick;
            adapter.DeleteItemClick += Adapter_DeleteItemClick;
            adapter.ManageChildrenItemClick += Adapter_ManageChildrenItemClick;

            fabPrompt = FindViewById<TextView>(Resource.Id.fabPrompt);

            RecyclerView recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            recyclerView.SetAdapter(adapter);

            ItemTouchHelper.Callback callback = new DragHelper(adapter);
            ItemTouchHelper touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(recyclerView);

            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);
            
                
            using (FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.createTaskFab))
            {
                fab.Click += Fab_Click;
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.MediaViewerMenu, menu);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override void OnBackPressed()
        {
            if (editingSubmitted)
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.WarningTitle)
                    .SetMessage(Resource.String.editActivityBackWarning)
                    .SetNegativeButton(Resource.String.dialog_cancel, (a, e) => { })
                    .SetPositiveButton(Resource.String.editActivityBackWarningAccept, (a, e) => { base.OnBackPressed(); })
                    .Show();
            }
            else
            {
                base.OnBackPressed();
            }
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == global::Android.Resource.Id.Home)
            {
                OnBackPressed();
                return true;
            }

            if (item.ItemId != Resource.Id.menudelete)
            {
                return base.OnOptionsItemSelected(item);
            }

            new global::Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(Resource.String.deleteTitle)
                .SetMessage(Resource.String.deleteMessage)
                .SetNegativeButton(Resource.String.dialog_cancel, (a, e) =>
                {
                })
                .SetPositiveButton(Resource.String.DeleteBtn, async (a, e) =>
                {
                    if (editingSubmitted)
                    {
                        ProgressDialog prog = new ProgressDialog(this);
                        prog.SetMessage(Resources.GetString(Resource.String.PleaseWait));
                        prog.Show();
                        ServerResponse<string> resp = await ServerUtils.Delete<string>("/api/learningactivities?id=" + newActivity.Id);
                        prog.Dismiss();

                        if (resp == null)
                        {
                            var suppress = AndroidUtils.ReturnToSignIn(this);
                            Toast.MakeText(this, Resource.String.ForceSignOut, ToastLength.Long).Show();
                            return;
                        }

                        if (resp.Success)
                        {
                            Toast.MakeText(this, Resource.String.uploadsUploadSuccessTitle, ToastLength.Long).Show();
                            MainMyCreationsFragment.ForceRefresh = true;
                            Finish();
                        }
                        else
                        {
                            Toast.MakeText(this, Resource.String.ConnectionError, ToastLength.Long).Show();
                        }
                    }
                    else
                    {
                        if (dbManager == null)
                        {
                            dbManager = await GetDatabaseManager();
                        }

                        var localActivities = JsonConvert.DeserializeObject<List<LearningActivity>>(dbManager.CurrentUser.LocalCreatedActivitiesJson);
                        localActivities.Remove(localActivities.FirstOrDefault(act => act.Id == newActivity.Id));
                        dbManager.CurrentUser.LocalCreatedActivitiesJson = JsonConvert.SerializeObject(localActivities);
                        dbManager.AddUser(dbManager.CurrentUser);
                        MainMyCreationsFragment.ForceRefresh = true;
                        Finish();
                    }
                })
                .Show();

            return true;
        }

        private void Adapter_ManageChildrenItemClick(object sender, int position)
        {
            SaveProgress();

            Intent manageChildTasksAct = new Intent(this, typeof(CreateManageChildTasksActivity));
            string json = JsonConvert.SerializeObject(newActivity, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                MaxDepth = 5
            });
            manageChildTasksAct.PutExtra("JSON", json);
            manageChildTasksAct.PutExtra("PARENT", position - 1);
            manageChildTasksAct.PutExtra("EDITING_SUBMITTED", editingSubmitted);
            StartActivityForResult(manageChildTasksAct, ManageChildrenIntent);
        }

        private void Adapter_DeleteItemClick(object sender, int position)
        {
            // Confirm task deletion
            new global::Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(Resource.String.deleteTitle)
                .SetMessage(Resource.String.deleteMessage)
                .SetNegativeButton(Resource.String.dialog_cancel, (a, e) => { })
                .SetPositiveButton(Resource.String.DeleteBtn, (a, b) =>
                {
                    position--;
                    adapter.Data.RemoveAt(position);
                    adapter.NotifyDataSetChanged();
                    SaveProgress();
                })
                .Show();
        }

        private void Adapter_EditItemClick(object sender, int position)
        {
            LearningTask thisTask = adapter.Data[position - 1];
            if (thisTask?.TaskType == null) return;

            Type activityType = AndroidUtils.GetTaskCreationActivityType(thisTask.TaskType.IdName);
            Intent intent = new Intent(this, activityType);
            string json = JsonConvert.SerializeObject(thisTask);
            intent.PutExtra("EDIT", json);

            List<LearningTask> tasksToPass = new List<LearningTask>(adapter.Data);
            tasksToPass.RemoveAt(position - 1);
            intent.PutExtra("CURRENT_TASKS", JsonConvert.SerializeObject(tasksToPass));

            StartActivityForResult(intent, EditTaskIntent);
        }

        private void Adapter_FinishClick(object sender, int e)
        {
            Intent intent = new Intent(this, typeof(CreateFinishActivity));

            for (int i = 0; i < adapter.Data.Count; i++)
            {
                adapter.Data[i].Order = i;
            }

            newActivity.LearningTasks = adapter.Data;
            intent.PutExtra("JSON", JsonConvert.SerializeObject(newActivity));
            intent.PutExtra("EDITING_SUBMITTED", editingSubmitted);
            StartActivity(intent);
        }

        private void Adapter_EditActivityClick(object sender, int e)
        {
            // Edit the activity's basic details
            Intent intent = new Intent(this, typeof(CreateNewActivity));
            newActivity.LearningTasks = adapter.Data;
            intent.PutExtra("JSON", JsonConvert.SerializeObject(newActivity));
            StartActivityForResult(intent, EditActivityIntent);
        }

        public async void SaveProgress()
        {
            newActivity.LearningTasks = adapter.Data;

            // Hide the prompt if the user has added a task
            fabPrompt.Visibility =
                (newActivity.LearningTasks != null && newActivity.LearningTasks.Any())
                    ? ViewStates.Gone : ViewStates.Visible;

            // Don't save changes to uploaded activities until we're ready to submit
            if (editingSubmitted) return;

            if (dbManager == null)
            {
                dbManager = await GetDatabaseManager();
            }

            // Add/update this new activity in the user's inprogress cache
            string cacheJson = dbManager.CurrentUser.LocalCreatedActivitiesJson;
            List<LearningActivity> inProgress = (string.IsNullOrWhiteSpace(cacheJson)) ?
                new List<LearningActivity>() :
                JsonConvert.DeserializeObject<List<LearningActivity>>(cacheJson);

            int existingInd = inProgress.FindIndex((la) => la.Id == newActivity.Id);

            if (existingInd == -1)
            {
                inProgress.Insert(0, newActivity);
            }
            else
            {
                inProgress.RemoveAt(existingInd);
                inProgress.Insert(0, newActivity);
            }

            dbManager.CurrentUser.LocalCreatedActivitiesJson = JsonConvert.SerializeObject(inProgress);
            dbManager.AddUser(dbManager.CurrentUser);
            MainMyCreationsFragment.ForceRefresh = true;
        }

        protected override void OnResume()
        {
            SaveProgress();
            adapter?.NotifyDataSetChanged();
            base.OnResume();
        }

        private void Fab_Click(object sender, EventArgs e)
        {
            using (Intent intent = new Intent(this, typeof(CreateChooseTaskTypeActivity)))
            {
                StartActivityForResult(intent, AddTaskIntent);
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] global::Android.App.Result resultCode, Intent data)
        {
            if (resultCode != Result.Ok) return;

            switch (requestCode)
            {
                case AddTaskIntent:
                    {
                        LearningTask newTask = JsonConvert.DeserializeObject<LearningTask>(data.GetStringExtra("JSON"));
                        adapter.Data.Add(newTask);
                        adapter.NotifyDataSetChanged();
                        break;
                    }
                case ManageChildrenIntent:
                    {
                        List<LearningTask> childTasks = JsonConvert.DeserializeObject<List<LearningTask>>(data.GetStringExtra("JSON"));
                        int parentInd = data.GetIntExtra("PARENT", -1);
                        if (parentInd >= 0)
                        {
                            newActivity.LearningTasks.ToList()[parentInd].ChildTasks = childTasks;
                            adapter.UpdateActivity(newActivity);
                        }

                        break;
                    }
                case EditActivityIntent:
                    {
                        LearningActivity returned = JsonConvert.DeserializeObject<LearningActivity>(data.GetStringExtra("JSON"));
                        if (returned != null)
                        {
                            newActivity = returned;
                            adapter.UpdateActivity(returned);
                        }

                        break;
                    }
                case EditTaskIntent:
                    {
                        LearningTask returned = JsonConvert.DeserializeObject<LearningTask>(data.GetStringExtra("JSON"));
                        int foundIndex = adapter.Data.FindIndex((LearningTask t) => t.Id == returned.Id);
                        if (foundIndex != -1)
                        {
                            adapter.Data[foundIndex] = returned;
                            adapter.NotifyDataSetChanged();
                        }

                        break;
                    }
            }
        }

    }
}