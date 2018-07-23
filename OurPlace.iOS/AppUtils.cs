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
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AVFoundation;
using CoreGraphics;
using OurPlace.Common.LocalData;
using OurPlace.Common.Models;
using UIKit;

namespace OurPlace.iOS
{
    public static class AppUtils
    {
        public static UIColor AppMainColour = UIColor.FromRGB(140, 192, 77);
        public static UIColor AppLightColour = UIColor.FromRGB(139, 195, 74);
        public static UIColor AppDarkColour = UIColor.FromRGB(128, 175, 70);
        public static UIColor AppAltColour = UIColor.FromRGB(192, 77, 140);

        public static async Task SignOut(UIViewController context)
        {
            Storage.CleanCache();
            await (context.ParentViewController as MainTabBarController).UpdateUploadsBadge(null);
            (await Storage.GetDatabaseManager()).CleanDatabase();

            // Divert to login screen
            var storyBoard = UIStoryboard.FromName("Main", null);
            context.NavigationController.PushViewController(storyBoard.InstantiateViewController("LoginController"), false);
        }

        public static async Task OpenActivity(LearningActivity act, UIStoryboard storyboard, UINavigationController navController)
        {

            // Save this activity to the database for showing in the 'recent' feed section
            (await Storage.GetDatabaseManager()).AddActivity(act);

            ActivityController taskController = storyboard.InstantiateViewController("ActivityController") as ActivityController;
            taskController.DisplayedActivity = act;
            navController.PushViewController(taskController, true);
        }

        public static string GetPathForLocalFile(string path)
        {
            return Path.Combine(Storage.GetCacheFolder(), path);
        }

        public static async Task<bool> AuthorizeCamera()
        {
            var authStatus = AVCaptureDevice.GetAuthorizationStatus(AVMediaType.Video);
            if (authStatus != AVAuthorizationStatus.Authorized)
            {
                return await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVMediaType.Video);
            }
            return true;
        }

        public static async Task<bool> AuthorizeMic()
        {
            var authStatus = AVCaptureDevice.GetAuthorizationStatus(AVMediaType.Audio);
            if (authStatus != AVAuthorizationStatus.Authorized)
            {
                return await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVMediaType.Audio);
            }
            return true;
        }

        public static void ShowSimpleDialog(UIViewController viewController, string title, string message, string buttonText, Action<UIAlertAction> completionHandler = null)
        {
            var okAlertController = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);
            okAlertController.AddAction(UIAlertAction.Create(buttonText, UIAlertActionStyle.Default, completionHandler));
            viewController.PresentViewController(okAlertController, true, null);
        }

        public static void ShowChoiceDialog<T>(UIViewController viewController, string title, string message, string choice1,
                                               Action<T> choice1Action, string choice2, Action<T> choice2Action, T data)
        {
            var alertController = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);
            alertController.AddAction(UIAlertAction.Create(choice1, UIAlertActionStyle.Default, (obj) =>
            {
                choice1Action?.Invoke(data);
            }));
            alertController.AddAction(UIAlertAction.Create(choice2, (choice2Action != null) ? UIAlertActionStyle.Default : UIAlertActionStyle.Cancel, (obj) =>
            {
                choice2Action?.Invoke(data);
            }));
            viewController.PresentViewController(alertController, true, null);
        }

        public static void ShowThreeChoiceDialog<T>(UIViewController viewController, string title, string message, string choice1,
                                                    Action<T> choice1Action, string choice2, Action<T> choice2Action,
                                                    string choice3, Action<T> choice3Action, T data)
        {
            var alertController = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);
            alertController.AddAction(UIAlertAction.Create(choice1, UIAlertActionStyle.Default, (obj) =>
            {
                choice1Action?.Invoke(data);
            }));
            alertController.AddAction(UIAlertAction.Create(choice2, UIAlertActionStyle.Default, (obj) =>
            {
                choice2Action?.Invoke(data);
            }));
            alertController.AddAction(UIAlertAction.Create(choice3, (choice3Action != null) ? UIAlertActionStyle.Default : UIAlertActionStyle.Cancel, (obj) =>
            {
                choice3Action?.Invoke(data);
            }));
            viewController.PresentViewController(alertController, true, null);
        }

        //https://stackoverflow.com/questions/10600613/ios-image-orientation-has-strange-behavior
        public static UIImage ScaleAndRotateImage(UIImage img, int maxRes = 1920)
        {
            CGImage imgRef = img.CGImage;

            float width = imgRef.Width;
            float height = imgRef.Height;
            CGRect bounds;

            if (width > maxRes || height > maxRes)
            {
                float ratio = width / height;

                if (ratio > 1)
                {
                    bounds = new CGRect(0, 0, maxRes, maxRes / ratio);
                }
                else
                {
                    bounds = new CGRect(0, 0, maxRes * ratio, maxRes);
                }
            }
            else
            {
                bounds = new CGRect(0, 0, width, height);
            }

            CGAffineTransform transform;
            float scaleRatio = (float)bounds.Size.Width / width;
            CGSize imgSize = new CGSize(imgRef.Width, imgRef.Height);
            UIImageOrientation orient = img.Orientation;

            switch (orient)
            {
                case UIImageOrientation.Up:
                    transform = CGAffineTransform.MakeIdentity();
                    break;
                case UIImageOrientation.UpMirrored:
                    transform = CGAffineTransform.MakeTranslation(imgSize.Width, 0f);
                    transform = CGAffineTransform.Scale(transform, -1, 1);
                    break;
                case UIImageOrientation.Down:
                    transform = CGAffineTransform.MakeTranslation(imgSize.Width, imgSize.Height);
                    transform = CGAffineTransform.Rotate(transform, (float)Math.PI);
                    break;
                case UIImageOrientation.DownMirrored:
                    transform = CGAffineTransform.MakeTranslation(0f, imgSize.Height);
                    transform = CGAffineTransform.Scale(transform, 1, -1);
                    break;
                case UIImageOrientation.LeftMirrored:
                    bounds = new CGRect(0, 0, bounds.Size.Height, bounds.Size.Width);
                    transform = CGAffineTransform.MakeTranslation(imgSize.Height, imgSize.Width);
                    transform = CGAffineTransform.Scale(transform, -1, 1);
                    transform = CGAffineTransform.Rotate(transform, (float)(3 * Math.PI / 2));
                    break;
                case UIImageOrientation.Left:
                    bounds = new CGRect(0, 0, bounds.Size.Height, bounds.Size.Width);
                    transform = CGAffineTransform.MakeTranslation(0, imgSize.Width);
                    transform = CGAffineTransform.Rotate(transform, (float)(3 * Math.PI / 2));
                    break;
                case UIImageOrientation.RightMirrored:
                    bounds = new CGRect(0, 0, bounds.Size.Height, bounds.Size.Width);
                    transform = CGAffineTransform.MakeScale(-1, 1);
                    transform = CGAffineTransform.Rotate(transform, (float)(Math.PI / 2));
                    break;
                case UIImageOrientation.Right:
                    bounds = new CGRect(0, 0, bounds.Size.Height, bounds.Size.Width);
                    transform = CGAffineTransform.MakeTranslation(imgSize.Height, 0);
                    transform = CGAffineTransform.Rotate(transform, (float)(Math.PI / 2));
                    break;
                default:
                    throw new Exception("Invalid image orientation: " + orient.ToString());
            }

            UIGraphics.BeginImageContext(bounds.Size);
            CGContext context = UIGraphics.GetCurrentContext();

            if (orient == UIImageOrientation.Right || orient == UIImageOrientation.Left)
            {
                context.ScaleCTM(-scaleRatio, scaleRatio);
                context.TranslateCTM(-height, 0);
            }
            else
            {
                context.ScaleCTM(scaleRatio, -scaleRatio);
                context.TranslateCTM(0, -height);
            }

            context.ConcatCTM(transform);
            context.DrawImage(new CGRect(0, 0, width, height), imgRef);

            UIImage imgCopy = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            return imgCopy;
        }

        // From Google Maps examples
        public static double GetRandomNumber()
        {
            var rng = new RNGCryptoServiceProvider();
            var bytes = new Byte[8];
            rng.GetBytes(bytes);
            var ul = BitConverter.ToUInt64(bytes, 0) / (1 << 11);
            return ul / (Double)(1UL << 53);
        }

    }
}
