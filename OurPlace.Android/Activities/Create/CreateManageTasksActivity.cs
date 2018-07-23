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
using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.V7.App;
using OurPlace.Common.Models;
using Android.Support.V7.Widget;
using OurPlace.Android.Adapters;
using Newtonsoft.Json;
using Android.Support.Design.Widget;
using OurPlace.Android.Fragments;
using Android.Support.V7.Widget.Helper;
using static OurPlace.Common.LocalData.Storage;
using OurPlace.Common.LocalData;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Add Tasks", Theme = "@style/OurPlaceActionBar")]
    public class CreateManageTasksActivity : AppCompatActivity
    {
        LearningActivity newActivity;
        RecyclerView recyclerView;
        RecyclerView.LayoutManager layoutManager;
        CreatedTasksAdapter adapter;
        TextView fabPrompt;
        DatabaseManager dbManager;
        int editTaskIntent = 198;
        int editActivityIntent = 199;
        int addTaskIntent = 200;
        int manageChildrenIntent = 201;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.CreateManageTasksActivity);

            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            newActivity = JsonConvert.DeserializeObject<LearningActivity>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            adapter = new CreatedTasksAdapter(this, newActivity, SaveProgress);
            adapter.EditActivityClick += Adapter_EditActivityClick;
            adapter.FinishClick += Adapter_FinishClick;
            adapter.EditItemClick += Adapter_EditItemClick;
            adapter.DeleteItemClick += Adapter_DeleteItemClick;
            adapter.ManageChildrenItemClick += Adapter_ManageChildrenItemClick;

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
            StartActivityForResult(manageChildTasksAct, manageChildrenIntent);
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

            List<LearningTask> tasksToPass = new List<LearningTask>(adapter.data);
            tasksToPass.RemoveAt(position - 1);
            intent.PutExtra("CURRENT_TASKS", JsonConvert.SerializeObject(tasksToPass));

            StartActivityForResult(intent, editTaskIntent);
        }

        private void Adapter_FinishClick(object sender, int e)
        {
            Intent intent = new Intent(this, typeof(CreateFinishActivity));

            for (int i = 0; i < adapter.data.Count(); i++)
            {
                adapter.data[i].Order = i;
            }

            newActivity.LearningTasks = adapter.data;
            intent.PutExtra("JSON", JsonConvert.SerializeObject(newActivity));
            StartActivity(intent);
            return;
        }

        private void Adapter_EditActivityClick(object sender, int e)
        {
            // Edit the activity's basic details
            Intent intent = new Intent(this, typeof(CreateNewActivity));
            newActivity.LearningTasks = adapter.data;
            intent.PutExtra("JSON", JsonConvert.SerializeObject(newActivity));
            StartActivityForResult(intent, editActivityIntent);
        }

        public async void SaveProgress()
        {
            if(dbManager == null)
            {
                dbManager = await GetDatabaseManager();
            }

            newActivity.LearningTasks = adapter.data;

            // Hide the prompt if the user has added a task
            fabPrompt.Visibility =
                (newActivity.LearningTasks != null && newActivity.LearningTasks.Count() > 0)
                ? ViewStates.Gone : ViewStates.Visible;

            // Add/update this new activity in the user's inprogress cache
            string cacheJson = dbManager.currentUser.LocalCreatedActivitiesJson;
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
            StartActivityForResult(intent, addTaskIntent);            
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] global::Android.App.Result resultCode, Intent data)
        {
            if(resultCode == global::Android.App.Result.Ok)
            {
                if(requestCode == addTaskIntent)
                {
                    LearningTask newTask = JsonConvert.DeserializeObject<LearningTask>(data.GetStringExtra("JSON"));
                    adapter.data.Add(newTask);
                    adapter.NotifyDataSetChanged();
                }
                else if(requestCode == manageChildrenIntent)
                {
                    List<LearningTask> childTasks = JsonConvert.DeserializeObject<List<LearningTask>>(data.GetStringExtra("JSON"));
                    int parentInd = data.GetIntExtra("PARENT", -1);
                    if(parentInd >= 0)
                    {
                        newActivity.LearningTasks.ToList()[parentInd].ChildTasks = childTasks;
                        adapter.UpdateActivity(newActivity);
                    }
                }
                else if(requestCode == editActivityIntent)
                {
                    LearningActivity returned = JsonConvert.DeserializeObject<LearningActivity>(data.GetStringExtra("JSON"));
                    if(returned != null)
                    {
                        newActivity = returned;
                        adapter.UpdateActivity(returned);
                    }
                }
                else if(requestCode == editTaskIntent)
                {
                    LearningTask returned = JsonConvert.DeserializeObject<LearningTask>(data.GetStringExtra("JSON"));
                    int foundIndex = adapter.data.FindIndex((LearningTask t) => t.Id == returned.Id);
                    if(foundIndex != -1)
                    {
                        adapter.data[foundIndex] = returned;
                        adapter.NotifyDataSetChanged();
                    }
                }
            }
        }

    }
}