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
    public class CreateTaskLocationMarker : AppCompatActivity
    {
        private EditText instructions;
        private TaskType taskType;
        private EditText minNumText;
        private EditText maxNumText;
        private CheckBox userLocOnlyCheckbox;

        private LearningTask newTask;
        private bool editing = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CreateTaskLocationMarker);
            instructions = FindViewById<EditText>(Resource.Id.taskInstructions);
            userLocOnlyCheckbox = FindViewById<CheckBox>(Resource.Id.checkboxUserLoc);
            minNumText = FindViewById<EditText>(Resource.Id.minNumMarkers);
            maxNumText = FindViewById<EditText>(Resource.Id.maxNumMarkers);
            Button addTaskBtn = FindViewById<Button>(Resource.Id.addTaskBtn);
            addTaskBtn.Click += AddTaskBtn_Click;
            minNumText.Text = "1";
            maxNumText.Text = "0";

            // Check if this is editing an existing task: if so, populate fields
            string editJson = Intent.GetStringExtra("EDIT") ?? "";
            newTask = JsonConvert.DeserializeObject<LearningTask>(editJson);

            if (newTask != null)
            {
                editing = true;
                taskType = newTask.TaskType;
                instructions.Text = newTask.Description;
                addTaskBtn.SetText(Resource.String.saveChanges);

                MapMarkerTaskData data = JsonConvert.DeserializeObject<MapMarkerTaskData>(newTask.JsonData);
                minNumText.Text = data.MinNumMarkers.ToString();
                maxNumText.Text = data.MaxNumMarkers.ToString();
                userLocOnlyCheckbox.Checked = data.UserLocationOnly;
            }
            else
            {
                string jsonData = Intent.GetStringExtra("JSON") ?? "";
                taskType = JsonConvert.DeserializeObject<TaskType>(jsonData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            }

            FindViewById<TextView>(Resource.Id.taskTypeNameText).Text = taskType.DisplayName;
            ImageViewAsync image = FindViewById<ImageViewAsync>(Resource.Id.taskIcon);
            AndroidUtils.LoadTaskTypeIcon(taskType, image);
        }

        private void AddTaskBtn_Click(object sender, EventArgs e)
        {
            int errMess = -1;
            int min = int.Parse(minNumText.Text);
            int max = int.Parse(maxNumText.Text);

            if (string.IsNullOrWhiteSpace(instructions.Text))
            {
                errMess = Resource.String.createNewActivityTaskInstruct;
            }
            else if (min < 1)
            {
                errMess = Resource.String.createNewMapMarkerErrMinLessThanOne;
            }
            else if(max < min && max != 0)
            {
                errMess = Resource.String.createNewMapMarkerErrMaxLessThanMin;
            }

            if(errMess != -1){
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.ErrorTitle)
                    .SetMessage(errMess)
                    .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                    .Show();
                return;
            }

            MapMarkerTaskData taskData = new MapMarkerTaskData
            {
                MaxNumMarkers = max,
                MinNumMarkers = min,
                UserLocationOnly = userLocOnlyCheckbox.Checked
            };

            if(newTask == null)
            {
                newTask = new LearningTask() { TaskType = taskType };
            }

            newTask.Description = instructions.Text;
            newTask.JsonData = JsonConvert.SerializeObject(taskData);

            string json = JsonConvert.SerializeObject(newTask, new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                MaxDepth = 5
            });

            Intent myIntent = (editing) ?
                new Intent(this, typeof(CreateActivityOverviewActivity)) :
                new Intent(this, typeof(CreateChooseTaskTypeActivity));

            myIntent.PutExtra("JSON", json);
            SetResult(global::Android.App.Result.Ok, myIntent);
            Finish();
        }
    }
}