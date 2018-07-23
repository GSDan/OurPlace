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
	[Register ("Create_ChildTasksOverviewController")]
	partial class Create_ChildTasksOverviewController
	{
		[Outlet]
		UIKit.UIButton FinishButton { get; set; }

		[Outlet]
		UIKit.UILabel headerLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (headerLabel != null) {
				headerLabel.Dispose ();
				headerLabel = null;
			}

			if (FinishButton != null) {
				FinishButton.Dispose ();
				FinishButton = null;
			}
		}
	}
}
