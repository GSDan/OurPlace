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
	[Register ("Create_EditPhotoMatchController")]
	partial class Create_EditPhotoMatchController
	{
		[Outlet]
		UIKit.UIButton ChooseImageButton { get; set; }

		[Outlet]
		UIKit.UIImageView ChosenImage { get; set; }

		[Outlet]
		UIKit.UIButton FinishButton { get; set; }

		[Outlet]
		UIKit.UITextView TaskDescription { get; set; }

		[Outlet]
		UIKit.UIImageView TaskTypeIcon { get; set; }

		[Outlet]
		UIKit.UILabel TaskTypeName { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (TaskTypeIcon != null) {
				TaskTypeIcon.Dispose ();
				TaskTypeIcon = null;
			}

			if (TaskTypeName != null) {
				TaskTypeName.Dispose ();
				TaskTypeName = null;
			}

			if (TaskDescription != null) {
				TaskDescription.Dispose ();
				TaskDescription = null;
			}

			if (ChosenImage != null) {
				ChosenImage.Dispose ();
				ChosenImage = null;
			}

			if (ChooseImageButton != null) {
				ChooseImageButton.Dispose ();
				ChooseImageButton = null;
			}

			if (FinishButton != null) {
				FinishButton.Dispose ();
				FinishButton = null;
			}
		}
	}
}
