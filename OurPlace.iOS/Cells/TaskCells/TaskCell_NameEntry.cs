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

using Foundation;
using UIKit;

namespace OurPlace.iOS
{
    public partial class TaskCell_NameEntry : UITableViewCell
    {
        public static readonly NSString Key = new NSString("TaskCell_NameEntry");
        public static readonly UINib Nib;

        private string enteredName;
        private Action OnNameEntered;

        static TaskCell_NameEntry()
        {
            Nib = UINib.FromName("TaskCell_NameEntry", NSBundle.MainBundle);
        }

        public TaskCell_NameEntry(IntPtr handle) : base(handle)
        {

        }

        public void UpdateContent(string display, Action OnNameEntered)
        {
            enteredName = display;
            this.OnNameEntered = OnNameEntered;

            if (string.IsNullOrWhiteSpace(display))
            {
                display = "This activity requires you to provide your name(s).";
            }

            NameLabel.Text = display;

            EditButton.TouchUpInside -= EditButtonPressed;
            EditButton.TouchUpInside += EditButtonPressed;
        }


        private void EditButtonPressed(object sender, EventArgs e)
        {
            OnNameEntered?.Invoke();
        }

    }
}
