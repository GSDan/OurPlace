using System;
using System.Drawing;

using Foundation;
using UIKit;

using PopColorPicker.iOS;

namespace PopColorPicker.iOS.Sample
{
    public partial class PopColorPickerSampleViewController : UIViewController
    {
        static bool UserInterfaceIdiomIsPhone
        {
            get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
        }

        public PopColorPickerSampleViewController(IntPtr handle)
            : base(handle)
        {
        }

        public override void DidReceiveMemoryWarning()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning();
			
            // Release any cached data, images, etc that aren't in use.
        }

        #region View lifecycle

        private PopColorPickerViewController _colorPickerViewController;
        private UIPopoverController _popoverController;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.

            _colorPickerViewController = new PopColorPickerViewController();

            _colorPickerViewController.CancelButton.Clicked += (object sender, EventArgs e) =>
            {
                if (UserInterfaceIdiomIsPhone)
                {
                    DismissViewController(true, null);
                }
                else
                {
                    _popoverController.Dismiss(true);
                }

                Console.WriteLine("Cancel Action 1");
            };

            _colorPickerViewController.DoneButton.Clicked += (object sender, EventArgs e) =>
            {
                if (UserInterfaceIdiomIsPhone)
                {
                    DismissViewController(true, null);
                }
                else
                {
                    _popoverController.Dismiss(true);
                }

                this.View.BackgroundColor = _colorPickerViewController.SelectedColor;
                Console.WriteLine("Done Action 1");
            };

            button1.TouchUpInside += Button1_TouchUpInside;
        }

        void Button1_TouchUpInside(object sender, EventArgs e)
        {
            if (UserInterfaceIdiomIsPhone)
            {
                var navController = new UINavigationController(_colorPickerViewController);
                PresentViewController(navController, true, null);
            }
            else
            {
                var navController = new UINavigationController(_colorPickerViewController);

                _popoverController = new UIPopoverController(navController);
                _popoverController.PresentFromRect(((UIButton)sender).Frame, View, UIPopoverArrowDirection.Up, true);
            }
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
        }

        #endregion
    }
}

