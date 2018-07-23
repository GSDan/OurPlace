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
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace OurPlace.iOS
{
	[Register ("Create_FinishController")]
	partial class Create_FinishController
	{
		[Outlet]
		UIKit.UIButton chooseLocationButton { get; set; }

		[Outlet]
		UIKit.UIButton finishButton { get; set; }

		[Outlet]
		UIKit.UISwitch isPublicSwitch { get; set; }

		[Outlet]
		UIKit.UILabel locationLabel { get; set; }

		[Outlet]
		UIKit.UISwitch reqNamesSwitch { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (chooseLocationButton != null) {
				chooseLocationButton.Dispose ();
				chooseLocationButton = null;
			}

			if (finishButton != null) {
				finishButton.Dispose ();
				finishButton = null;
			}

			if (isPublicSwitch != null) {
				isPublicSwitch.Dispose ();
				isPublicSwitch = null;
			}

			if (locationLabel != null) {
				locationLabel.Dispose ();
				locationLabel = null;
			}

			if (reqNamesSwitch != null) {
				reqNamesSwitch.Dispose ();
				reqNamesSwitch = null;
			}
		}
	}
}
