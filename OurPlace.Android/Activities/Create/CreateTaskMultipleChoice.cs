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
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using FFImageLoading.Views;
using Newtonsoft.Json;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "New Task", Theme = "@style/OurPlaceActionBar")]
    public class CreateTaskMultipleChoice : AppCompatActivity
    {
        private EditText instructions;
        private TaskType taskType;
        private LinearLayout choicesRoot;
        private EditText newEntryText;
        private TextView optionsHeader;
        private List<string> entries;
        private Random rand;

        private LearningTask newTask;
        private bool editing = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CreateTaskMultipleChoice);
            newEntryText = FindViewById<EditText>(Resource.Id.newOptionText);
            choicesRoot = FindViewById<LinearLayout>(Resource.Id.multiChoiceRoot);
            optionsHeader = FindViewById<TextView>(Resource.Id.createdChoicesHeader);
            optionsHeader.Text = Resources.GetString(Resource.String.createNewMultChoiceNone);
            FindViewById<Button>(Resource.Id.addOptionBtn).Click += CreateTaskMultipleChoice_Click;
            Button addTaskBtn = FindViewById<Button>(Resource.Id.addTaskBtn);
            addTaskBtn.Click += AddTaskBtn_Click;
            instructions = FindViewById<EditText>(Resource.Id.taskInstructions);

            rand = new Random();
            entries = new List<string>();

            // Check if this is editing an existing task: if so, populate fields
            string editJson = Intent.GetStringExtra("EDIT") ?? "";
            newTask = JsonConvert.DeserializeObject<LearningTask>(editJson);

            if (newTask != null)
            {
                addTaskBtn.SetText(Resource.String.saveChanges);

                editing = true;
                taskType = newTask.TaskType;
                instructions.Text = newTask.Description;
                List<string> existingEntries = JsonConvert.DeserializeObject<List<string>>(newTask.JsonData);

                foreach (string entry in existingEntries)
                {
                    AddChoice(entry);
                }
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

        private void CreateTaskMultipleChoice_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(newEntryText.Text))
            {
                return;
            }

            AddChoice(newEntryText.Text);
            newEntryText.Text = "";
        }

        private void AddChoice(string choice)
        {
            View child = LayoutInflater.Inflate(Resource.Layout.CreateTaskMultipleChoiceEntry, null);
            child.FindViewById<TextView>(Resource.Id.option).Text = choice;
            child.FindViewById<ImageButton>(Resource.Id.deleteButton).Click += DeleteItemClicked;
            child.Id = rand.Next();

            optionsHeader.Text = Resources.GetString(Resource.String.createNewMultChoiceAdded);
            choicesRoot.AddView(child);

            entries.Add(choice);
        }

        private void DeleteItemClicked(object sender, EventArgs e)
        {
            ViewGroup parent = (ViewGroup)((ImageButton)sender).Parent;

            for (int i = 0; i < parent.ChildCount; i++)
            {
                View child = parent.GetChildAt(i);
                if (child.GetType() == typeof(AppCompatTextView))
                {
                    entries.Remove(((TextView)child).Text);
                    break;
                }
            }

            ViewGroup topParent = (ViewGroup)parent.Parent.Parent;

            for (int i = 0; i < choicesRoot.ChildCount; i++)
            {
                View child = choicesRoot.GetChildAt(i);
                if (child.Id == topParent.Id)
                {
                    choicesRoot.RemoveViewAt(i);
                    break;
                }
            }

            if (choicesRoot.ChildCount <= 1)
            {
                optionsHeader.Text = Resources.GetString(Resource.String.createNewMultChoiceNone);
            }
        }

        private void AddTaskBtn_Click(object sender, EventArgs e)
        {
            int errMess = -1;

            if (string.IsNullOrWhiteSpace(instructions.Text))
            {
                errMess = Resource.String.createNewActivityTaskInstruct;
            }

            if (entries.Count < 2)
            {
                errMess = Resource.String.createNewMultChoiceTooFew;
            }

            if (errMess != -1)
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.ErrorTitle)
                    .SetMessage(errMess)
                    .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                    .Show();
                return;
            }

            if (newTask == null)
            {
                newTask = new LearningTask() { TaskType = taskType };
            }

            newTask.Description = instructions.Text;
            newTask.JsonData = JsonConvert.SerializeObject(entries);

            string json = JsonConvert.SerializeObject(newTask, new JsonSerializerSettings
            {
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