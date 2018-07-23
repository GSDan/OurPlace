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
using System.Linq;
using FFImageLoading;
using Foundation;
using Newtonsoft.Json;
using OurPlace.Common.Models;
using UIKit;

namespace OurPlace.iOS
{
    public partial class TaskCell_Simple : UITableViewCell
    {
        public static readonly NSString Key = new NSString("TaskCell_Simple");
        public static readonly UINib Nib;
		private AppTask taskData;
        private Action<AppTask> startTask;
        private NSLayoutConstraint showChildTeaseConstraint;
        private NSLayoutConstraint hideChildTeaseConstraint;

        static TaskCell_Simple()
        {
            Nib = UINib.FromName("TaskCell_Simple", NSBundle.MainBundle);
        }

        protected TaskCell_Simple(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

		public void UpdateContent(AppTask data, Action<AppTask> OnButtonClicked)
        {
            taskData = data;
            startTask = OnButtonClicked;

            TaskType.Text = data.TaskType.DisplayName;
            TaskDescription.Text = data.Description;

            if (string.IsNullOrWhiteSpace(data.TaskType.IconUrl))
            {
                ImageService.Instance.LoadCompiledResource("AppLogo").Into(TaskTypeIcon);
            }
            else
            {
                ImageService.Instance.LoadUrl(data.TaskType.IconUrl)
                //.LoadingPlaceholder("AppLogo", ImageSource.CompiledResource)
                            .Into(TaskTypeIcon);
            }

            // Show tease label if this task has children and hasn't yet been completed
            if (data.ChildTasks != null && data.ChildTasks.Count() > 0 && !data.IsCompleted)
            {
                NSLayoutConstraint.DeactivateConstraints(new NSLayoutConstraint[] { hideChildTeaseConstraint });
                NSLayoutConstraint.ActivateConstraints(new NSLayoutConstraint[] { showChildTeaseConstraint });
                ChildTease.Text = string.Format(
               "Complete this to view {0} locked task{1}!",
                    data.ChildTasks.Count(),
               data.ChildTasks.Count() > 1 ? "s" : "");
            }
            else
            {
                NSLayoutConstraint.DeactivateConstraints(new NSLayoutConstraint[] { showChildTeaseConstraint });
                NSLayoutConstraint.ActivateConstraints(new NSLayoutConstraint[] { hideChildTeaseConstraint });
            }
         
            // Make child tasks visually distinct
            Layer.BackgroundColor = (data.IsChild) ? UIColor.FromRGBA(140, 192, 77, 50).CGColor : UIColor.White.CGColor;

            if (data.CompletionData != null && !string.IsNullOrWhiteSpace(data.CompletionData.JsonData))
            {
                if (data.TaskType.IdName == "ENTER_TEXT")
                {
                    TaskDescription.Text += string.Format("\n\nYour response:\n\'{0}\'", data.CompletionData.JsonData);
                    StartButton.SetTitle("Edit", UIControlState.Normal);
                }
                else if(data.TaskType.IdName == "MULT_CHOICE")
                {
                    string[] choices = JsonConvert.DeserializeObject<string[]>(data.JsonData);
                    TaskDescription.Text += string.Format("\n\nYour response:\n\'{0}\'",  choices[int.Parse(data.CompletionData.JsonData)]);
                }
                else if (data.TaskType.IdName == "MAP_MARK")
                {
                    Map_Location[] locs = JsonConvert.DeserializeObject<Map_Location[]>(data.CompletionData.JsonData);
                    TaskDescription.Text += string.Format("\n\nYou've marked {0} location{1}", locs.Length, (locs.Length > 1) ? "s" : "");
                    StartButton.SetTitle("Edit", UIControlState.Normal);
                }
            }
        }
      
        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            float screenWidth = (float)UIScreen.MainScreen.Bounds.Width;
            float cellWidth = (screenWidth - 10) / 1.4f;
         
            showChildTeaseConstraint = NSLayoutConstraint.Create(ChildTease, NSLayoutAttribute.Height, NSLayoutRelation.Equal, 1, 15);
            showChildTeaseConstraint.Active = false;
            hideChildTeaseConstraint = NSLayoutConstraint.Create(ChildTease, NSLayoutAttribute.Height, NSLayoutRelation.Equal, 1, 0);
            hideChildTeaseConstraint.Active = true;
         
            StartButton.TouchUpInside += (sender, e) => {
                if (taskData != null && startTask != null)
                {
                    startTask(taskData);
                }
            };

            NSLayoutConstraint.ActivateConstraints(new NSLayoutConstraint[] { hideChildTeaseConstraint });
        }
    }
}
