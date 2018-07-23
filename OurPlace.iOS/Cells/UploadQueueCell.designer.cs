// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace ParkLearn.iOS
{
	[Register ("UploadQueueCell")]
	partial class UploadQueueCell
	{
		[Outlet]
		UIKit.UIImageView ActivityIcon { get; set; }

		[Outlet]
		UIKit.UILabel DateLabel { get; set; }

		[Outlet]
		UIKit.UILabel FileSizeLabel { get; set; }

		[Outlet]
		UIKit.UILabel TitleLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (TitleLabel != null) {
				TitleLabel.Dispose ();
				TitleLabel = null;
			}

			if (DateLabel != null) {
				DateLabel.Dispose ();
				DateLabel = null;
			}

			if (FileSizeLabel != null) {
				FileSizeLabel.Dispose ();
				FileSizeLabel = null;
			}

			if (ActivityIcon != null) {
				ActivityIcon.Dispose ();
				ActivityIcon = null;
			}
		}
	}
}
