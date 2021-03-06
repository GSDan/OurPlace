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

// This file has been autogenerated from a class added in the UI designer.

using System;
using FFImageLoading;
using Foundation;
using OurPlace.Common.Models;
using UIKit;

namespace OurPlace.iOS
{
    public partial class TaskTypeCell : UITableViewCell
    {
        public static readonly NSString Key = new NSString("TaskTypeCell");

        private TaskType taskType;
        private Action<TaskType> onClicked;

        public TaskTypeCell(IntPtr handle) : base(handle)
        {
        }

        public void UpdateContent(TaskType data, Action<TaskType> OnButtonClicked)
        {
            NameLabel.Text = data.DisplayName;
            DescriptionLabel.Text = data.Description;

            taskType = data;
            onClicked = OnButtonClicked;

            if (string.IsNullOrWhiteSpace(data.IconUrl))
            {
                ImageService.Instance.LoadCompiledResource("AppLogo").Into(Icon);
            }
            else
            {
                ImageService.Instance.LoadUrl(data.IconUrl).Into(Icon);
            }

            AddButton.TouchUpInside -= Chosen;
            AddButton.TouchUpInside += Chosen;
        }

        private void Chosen(object sender, EventArgs e)
        {
            onClicked(taskType);
        }
    }
}
