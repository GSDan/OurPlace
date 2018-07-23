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
	[Register ("CreateMultipleChoiceCell")]
	partial class CreateMultipleChoiceCell
	{
		[Outlet]
		UIKit.UILabel AnswerLabel { get; set; }

		[Outlet]
		UIKit.UIButton DeleteButton { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (AnswerLabel != null) {
				AnswerLabel.Dispose ();
				AnswerLabel = null;
			}

			if (DeleteButton != null) {
				DeleteButton.Dispose ();
				DeleteButton = null;
			}
		}
	}
}
