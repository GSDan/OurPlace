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
using Android.Support.V7.App;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Views;
using Newtonsoft.Json;
using OurPlace.Common.Models;
using System;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "New Task", Theme = "@style/OurPlaceActionBar")]
    public class CreateTaskBasic : AppCompatActivity
    {
        private EditText instructions;
        private TaskType taskType;
        private LearningTask newTask;
        private bool editing = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Check if this is editing an existing task: if so, populate fields
            string editJson = Intent.GetStringExtra("EDIT") ?? "";
            newTask = JsonConvert.DeserializeObject<LearningTask>(editJson);

            // otherwise, we'll use basic tasktype info
            string jsonData = Intent.GetStringExtra("JSON") ?? "";
            taskType = JsonConvert.DeserializeObject<TaskType>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            if((taskType != null && taskType.IdName == "SCAN_QR") || 
                (newTask != null && newTask.TaskType.IdName == "SCAN_QR"))
            {
                SetContentView(Resource.Layout.CreateTaskScanQR);
            }
            else
            {
                SetContentView(Resource.Layout.CreateTask);
            }

            TextView taskTypeName = FindViewById<TextView>(Resource.Id.taskTypeNameText);
            ImageViewAsync image = FindViewById<ImageViewAsync>(Resource.Id.taskIcon);
            Button addTaskBtn = FindViewById<Button>(Resource.Id.addTaskBtn);
            addTaskBtn.Click += AddTaskBtn_Click;
            instructions = FindViewById<EditText>(Resource.Id.taskInstructions);

            if (newTask != null)
            {
                instructions.Text = newTask.Description;
                taskType = newTask.TaskType;
                editing = true;
                addTaskBtn.SetText(Resource.String.saveChanges);
            }
            else
            {
                // If edit is null just use the tasktype JSON
                taskType = JsonConvert.DeserializeObject<TaskType>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
                newTask = new LearningTask();
                newTask.TaskType = taskType;
            }

            taskTypeName.Text = taskType.DisplayName;
            AndroidUtils.LoadTaskTypeIcon(taskType, image);
        }

        private void AddTaskBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(instructions.Text))
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.ErrorTitle)
                    .SetMessage(Resource.String.createNewActivityTaskInstruct)
                    .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                    .Show();
                return;
            }

            newTask.Description = instructions.Text;

            string json = JsonConvert.SerializeObject(newTask, new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                MaxDepth = 5
            });

            Intent myIntent = new Intent(this, typeof(CreateActivityOverviewActivity));
            myIntent.PutExtra("JSON", json);
            SetResult(Result.Ok, myIntent);
            Finish();
        }
    }
}