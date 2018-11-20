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
	[Register ("Create_EditTaskController")]
	partial class Create_EditTaskController
	{
		[Outlet]
		protected UIKit.UIButton FinishButton { get; private set; }

		[Outlet]
		protected UIKit.UITextView TaskDescription { get; private set; }

		[Outlet]
		protected UIKit.UIImageView TaskTypeIcon { get; private set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (FinishButton != null) {
				FinishButton.Dispose ();
				FinishButton = null;
			}

			if (TaskDescription != null) {
				TaskDescription.Dispose ();
				TaskDescription = null;
			}

			if (TaskTypeIcon != null) {
				TaskTypeIcon.Dispose ();
				TaskTypeIcon = null;
			}
		}
	}
}
