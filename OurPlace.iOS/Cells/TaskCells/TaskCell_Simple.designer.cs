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

// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace OurPlace.iOS
{
    [Register ("TaskCell_Simple")]
    partial class TaskCell_Simple
    {
        [Outlet]
        UIKit.UILabel ChildTease { get; set; }


        [Outlet]
        UIKit.UIButton StartButton { get; set; }


        [Outlet]
        UIKit.UILabel TaskDescription { get; set; }


        [Outlet]
        UIKit.UILabel TaskType { get; set; }


        [Outlet]
        UIKit.UIImageView TaskTypeIcon { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (ChildTease != null) {
                ChildTease.Dispose ();
                ChildTease = null;
            }

            if (StartButton != null) {
                StartButton.Dispose ();
                StartButton = null;
            }

            if (TaskDescription != null) {
                TaskDescription.Dispose ();
                TaskDescription = null;
            }

            if (TaskType != null) {
                TaskType.Dispose ();
                TaskType = null;
            }

            if (TaskTypeIcon != null) {
                TaskTypeIcon.Dispose ();
                TaskTypeIcon = null;
            }
        }
    }
}