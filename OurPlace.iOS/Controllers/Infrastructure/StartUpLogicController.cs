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
using Foundation;
using System;
using UIKit;
using System.Threading.Tasks;

namespace OurPlace.iOS
{
    public partial class StartUpLogicController : UINavigationController
    {
        public StartUpLogicController (IntPtr handle) : base (handle)
        {
            
        }

        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();

            if (!AppDelegate.IsInDesignerView)
            {
                await Initialize();   
            }
        }

        private async Task Initialize()
        {
            bool success = await Common.LocalData.Storage.InitializeLogin();
            var storyBoard = UIStoryboard.FromName("Main", null);
			if (success)
            {
                // Logged in already
                PushViewController(storyBoard.InstantiateViewController("MainTabController"), false);
                ChangeRootToHome();
            }
            else
            {
                // Divert to login screen
                PushViewController(storyBoard.InstantiateViewController("LoginController"), false);
            }
        }

        public void ChangeRootToHome()
        {
            var storyBoard = UIStoryboard.FromName("Main", null);
            var mainScreen = storyBoard.InstantiateViewController("MainTabController");

            UIApplication.SharedApplication.Delegate.GetWindow().RootViewController = new UINavigationController(mainScreen);

            //UIApplication.SharedApplication.KeyWindow.RootViewController = new UINavigationController(mainScreen);
            PopToRootViewController(true);
        }
    }
}