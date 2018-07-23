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
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Android.Adapters;
using OurPlace.Android.Fragments;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static OurPlace.Common.LocalData.Storage;
using System.Threading.Tasks;
using OurPlace.Common.LocalData;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Manage Follow-Up Tasks", Theme = "@style/OurPlaceActionBarAlt")]
    public class CreateManageChildTasksActivity : AppCompatActivity
    {
        LearningActivity learningActivity;
        LearningTask parentTask;
        RecyclerView recyclerView;
        RecyclerView.LayoutManager layoutManager;
        CreatedChildTasksAdapter adapter;
        TextView fabPrompt;
        DatabaseManager dbManager;
        int editTaskIntent = 198;
        int addTaskIntent = 200;
        int parentInd;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.CreateManageChildTasksActivity);

            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            parentInd = Intent.GetIntExtra("PARENT", -1);

            learningActivity = JsonConvert.DeserializeObject<LearningActivity>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            parentTask = learningActivity.LearningTasks.ElementAt(parentInd);

            adapter = new CreatedChildTasksAdapter(this, parentTask, SaveProgress);
            adapter.FinishClick += Adapter_FinishClick;
            adapter.EditItemClick += Adapter_EditItemClick;
            adapter.DeleteItemClick += Adapter_DeleteItemClick;

            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            recyclerView.SetAdapter(adapter);

            ItemTouchHelper.Callback callback = new DragHelper(adapter);
            ItemTouchHelper touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(recyclerView);

            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);

            fabPrompt = FindViewById<TextView>(Resource.Id.fabPrompt);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.createTaskFab);
            fab.Click += Fab_Click;
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
                    adapter.data.RemoveAt(position);
                    adapter.NotifyDataSetChanged();
                    SaveProgress();
                })
                .Show();
        }

        private void Adapter_EditItemClick(object sender, int position)
        {
            LearningTask thisTask = adapter.data[position - 1];
            if (thisTask == null || thisTask.TaskType == null) return;

            Type activityType = AndroidUtils.GetTaskCreationActivityType(thisTask.TaskType.IdName);
            Intent intent = new Intent(this, activityType);
            string json = JsonConvert.SerializeObject(thisTask);
            intent.PutExtra("EDIT", json);
            intent.PutExtra("PARENT", JsonConvert.SerializeObject(parentTask));

            StartActivityForResult(intent, editTaskIntent);
        }

        private void Adapter_FinishClick(object sender, int e)
        {
            for (int i = 0; i < adapter.data.Count(); i++)
            {
                adapter.data[i].Order = i;
            }

            string json = JsonConvert.SerializeObject(adapter.data, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                MaxDepth = 5
            });

            Intent myIntent = new Intent(this, typeof(CreateManageTasksActivity));
            myIntent.PutExtra("JSON", json);
            myIntent.PutExtra("PARENT", parentInd);
            SetResult(global::Android.App.Result.Ok, myIntent);
            Finish();
            return;
        }

        public async void SaveProgress()
        {
            if(dbManager == null)
            {
                dbManager = await GetDatabaseManager();
            }

            parentTask.ChildTasks = adapter.data;

            // Hide the prompt if the user has added a task
            fabPrompt.Visibility =
                (parentTask.ChildTasks != null && parentTask.ChildTasks.Count() > 0)
                ? ViewStates.Gone : ViewStates.Visible;

            // Add/update this new activity in the user's inprogress cache
            string cacheJson = dbManager.currentUser.LocalCreatedActivitiesJson;
            List<LearningActivity> inProgress = (string.IsNullOrWhiteSpace(cacheJson)) ?
                new List<LearningActivity>() :
                JsonConvert.DeserializeObject<List<LearningActivity>>(cacheJson);

            int existingInd = inProgress.FindIndex((la) => la.Id == learningActivity.Id);

            List<LearningTask> lt = learningActivity.LearningTasks.ToList();
            lt[parentInd] = parentTask;
            learningActivity.LearningTasks = lt;

            if (existingInd == -1)
            {
                inProgress.Insert(0, learningActivity);
            }
            else
            {
                inProgress.RemoveAt(existingInd);
                inProgress.Insert(0, learningActivity);
            }

            dbManager.currentUser.LocalCreatedActivitiesJson = JsonConvert.SerializeObject(inProgress);
            dbManager.AddUser(dbManager.currentUser);
            MainMyActivitiesFragment.Loaded = false;
        }

        protected override void OnResume()
        {
            SaveProgress();
            base.OnResume();
        }

        private void Fab_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(CreateChooseTaskTypeActivity));
            intent.PutExtra("PARENT", JsonConvert.SerializeObject(parentTask));
            StartActivityForResult(intent, addTaskIntent);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (resultCode == Result.Ok)
            {
                if (requestCode == addTaskIntent)
                {
                    LearningTask newTask = JsonConvert.DeserializeObject<LearningTask>(data.GetStringExtra("JSON"));
                    adapter.data.Add(newTask);
                    adapter.NotifyDataSetChanged();
                }
                else if (requestCode == editTaskIntent)
                {
                    LearningTask returned = JsonConvert.DeserializeObject<LearningTask>(data.GetStringExtra("JSON"));
                    int foundIndex = adapter.data.FindIndex(t => t.Id == returned.Id);
                    if (foundIndex != -1)
                    {
                        adapter.data[foundIndex] = returned;
                        adapter.NotifyDataSetChanged();
                    }
                }
                SaveProgress();
            }
        }
    }
}