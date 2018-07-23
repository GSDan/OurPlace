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
    [Register ("Create_ActivityMetaController")]
    partial class Create_ActivityMetaController
    {
        [Outlet]
        UIKit.UITextField ActivityDescription { get; set; }


        [Outlet]
        UIKit.UIImageView ActivityLogo { get; set; }


        [Outlet]
        UIKit.UITextField ActivityTitle { get; set; }


        [Outlet]
        UIKit.UIButton CancelButton { get; set; }


        [Outlet]
        UIKit.UIButton ContinueButton { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (ActivityDescription != null) {
                ActivityDescription.Dispose ();
                ActivityDescription = null;
            }

            if (ActivityLogo != null) {
                ActivityLogo.Dispose ();
                ActivityLogo = null;
            }

            if (ActivityTitle != null) {
                ActivityTitle.Dispose ();
                ActivityTitle = null;
            }

            if (CancelButton != null) {
                CancelButton.Dispose ();
                CancelButton = null;
            }

            if (ContinueButton != null) {
                ContinueButton.Dispose ();
                ContinueButton = null;
            }
        }
    }
}