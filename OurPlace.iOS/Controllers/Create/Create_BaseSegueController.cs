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
using System;
using CoreGraphics;
using Foundation;
using OurPlace.Common.Models;
using UIKit;
using OurPlace.iOS.Helpers;
using System.Drawing;

namespace OurPlace.iOS.Controllers.Create
{
    public abstract partial class Create_BaseSegueController : UIViewController
    {
        public LearningActivity thisActivity;
        public bool wasCancelled;

        protected UIToolbar keyboardToolbar;

        public Create_BaseSegueController(IntPtr handle) : base(handle)
        {
        }

        // https://gist.github.com/redent/7263276
        /// <summary>
        /// Call this method from constructor, ViewDidLoad or ViewWillAppear to enable keyboard handling in the main partial class
        /// </summary>
        protected void InitKeyboardHandling()
        {
            //Only do this if required
            if (HandlesKeyboardNotifications())
            {
                keyboardToolbar = new UIToolbar(new RectangleF(0.0f, 0.0f, (float)View.Frame.Size.Width, 44.0f));
                keyboardToolbar.TintColor = UIColor.White;
                keyboardToolbar.BarStyle = UIBarStyle.Black;
                keyboardToolbar.Translucent = true;

                keyboardToolbar.Items = new UIBarButtonItem[]{
                    new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
                    new UIBarButtonItem(UIBarButtonSystemItem.Done, delegate {
                        KeyboardGetActiveView().ResignFirstResponder();
                    })
                };

                RegisterForKeyboardNotifications();
            }
        }

        /// <summary>
        /// Set this field to any view inside the textfield to center this view instead of the current responder
        /// </summary>
        protected UIView ViewToCenterOnKeyboardShown;
        protected UIScrollView ScrollToCenterOnKeyboardShown;

        /// <summary>
        /// Override point for subclasses, return true if you want to handle keyboard notifications
        /// to center the active responder in the scroll above the keyboard when it appears
        /// </summary>
        public virtual bool HandlesKeyboardNotifications()
        {
            return false;
        }

        NSObject keyboardShowObserver;
        NSObject keyboardHideObserver;
        protected virtual void RegisterForKeyboardNotifications()
        {
            if (keyboardShowObserver == null)
            {
                keyboardShowObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, OnKeyboardNotification);
            }

            if (keyboardHideObserver == null)
            {
                keyboardHideObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, OnKeyboardNotification);
            }
        }

        protected virtual void UnregisterForKeyboardNotifications()
        {
            if (keyboardShowObserver != null)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(keyboardShowObserver);
                keyboardShowObserver.Dispose();
                keyboardShowObserver = null;
            }

            if (keyboardHideObserver != null)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(keyboardHideObserver);
                keyboardHideObserver.Dispose();
                keyboardHideObserver = null;
            }
        }

        /// <summary>
        /// Gets the UIView that represents the "active" user input control (e.g. textfield, or button under a text field)
        /// </summary>
        /// <returns>
        /// A <see cref="UIView"/>
        /// </returns>
        protected virtual UIView KeyboardGetActiveView()
        {
            return View.FindFirstResponder();
        }

        private void OnKeyboardNotification(NSNotification notification)
        {
            if (!IsViewLoaded) return;

            //Check if the keyboard is becoming visible
            var visible = notification.Name == UIKeyboard.WillShowNotification;

            //Start an animation, using values from the keyboard
            UIView.BeginAnimations("AnimateForKeyboard");
            UIView.SetAnimationBeginsFromCurrentState(true);
            UIView.SetAnimationDuration(UIKeyboard.AnimationDurationFromNotification(notification));
            UIView.SetAnimationCurve((UIViewAnimationCurve)UIKeyboard.AnimationCurveFromNotification(notification));

            //Pass the notification, calculating keyboard height, etc.
            var keyboardFrame = visible
                ? UIKeyboard.FrameEndFromNotification(notification)
                : UIKeyboard.FrameBeginFromNotification(notification);

            OnKeyboardChanged(visible, keyboardFrame);

            //Commit the animation
            UIView.CommitAnimations();
        }

        /// <summary>
        /// Override this method to apply custom logic when the keyboard is shown/hidden
        /// </summary>
        protected virtual void OnKeyboardChanged(bool keyboardIsVisible, CGRect keyboardFrame)
        {
            var activeView = ViewToCenterOnKeyboardShown ?? KeyboardGetActiveView();
            if (activeView == null)
                return;

            var scrollView = ScrollToCenterOnKeyboardShown ??
                activeView.FindTopSuperviewOfType(View, typeof(UIScrollView)) as UIScrollView;
            if (scrollView == null)
                return;

            if (!keyboardIsVisible)
            {
                scrollView.RestoreScrollPosition();
            }
            else
            {
                if (activeView.GetType() == typeof(UITextView))
                {
                    ((UITextView)activeView).InputAccessoryView = keyboardToolbar;
                }

                scrollView.CenterView(activeView, keyboardFrame);
            }

        }

        /// <summary>
        /// Call it to force dismiss keyboard when background is tapped
        /// </summary>
        protected void DismissKeyboardOnBackgroundTap()
        {
            // Add gesture recognizer to hide keyboard
            var tap = new UITapGestureRecognizer { CancelsTouchesInView = false };
            tap.AddTarget(() => View.EndEditing(true));
            tap.ShouldReceiveTouch = (recognizer, touch) =>
                !(touch.View is UIControl || touch.View.FindSuperviewOfType(View, typeof(UITableViewCell)) != null);
            View.AddGestureRecognizer(tap);
        }
    }
}
