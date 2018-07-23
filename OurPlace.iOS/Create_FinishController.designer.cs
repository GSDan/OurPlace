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
		UIKit.UISwitch reqNameSwitch { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (locationLabel != null) {
				locationLabel.Dispose ();
				locationLabel = null;
			}

			if (chooseLocationButton != null) {
				chooseLocationButton.Dispose ();
				chooseLocationButton = null;
			}

			if (isPublicSwitch != null) {
				isPublicSwitch.Dispose ();
				isPublicSwitch = null;
			}

			if (reqNameSwitch != null) {
				reqNameSwitch.Dispose ();
				reqNameSwitch = null;
			}

			if (finishButton != null) {
				finishButton.Dispose ();
				finishButton = null;
			}
		}
	}
}
