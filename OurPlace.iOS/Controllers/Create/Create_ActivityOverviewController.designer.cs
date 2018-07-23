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
    [Register ("Create_ActivityOverviewController")]
    partial class Create_ActivityOverviewController
    {
        [Outlet]
        UIKit.UILabel ActivityDescription { get; set; }


        [Outlet]
        UIKit.UIImageView ActivityImage { get; set; }


        [Outlet]
        UIKit.UILabel ActivityName { get; set; }


        [Outlet]
        UIKit.UIButton EditMetaButton { get; set; }


        [Outlet]
        UIKit.UIButton FooterButton { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (ActivityDescription != null) {
                ActivityDescription.Dispose ();
                ActivityDescription = null;
            }

            if (ActivityImage != null) {
                ActivityImage.Dispose ();
                ActivityImage = null;
            }

            if (ActivityName != null) {
                ActivityName.Dispose ();
                ActivityName = null;
            }

            if (EditMetaButton != null) {
                EditMetaButton.Dispose ();
                EditMetaButton = null;
            }

            if (FooterButton != null) {
                FooterButton.Dispose ();
                FooterButton = null;
            }
        }
    }
}