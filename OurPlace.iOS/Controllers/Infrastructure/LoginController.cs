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
using OurPlace.Common;
using OurPlace.Common.Models;
using OurPlace.iOS.Helpers;
using SafariServices;
using System.Threading.Tasks;
using CoreGraphics;
using System.Linq;
using OurPlace.Common.LocalData;

namespace OurPlace.iOS
{
    public partial class LoginController : UIViewController
    {
        private ExternalLogin[] providers;
        private LoadingOverlay loadPop;
        private NSObject notificationToken;
        private SFSafariViewController safariController;

        private string accessToken;
        private DateTime accessExpiresAt;
        private string refreshToken;
        private DateTime refreshExpiresAt;

        public LoginController(IntPtr handle) : base(handle)
        {

        }

        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();

            if (!AppDelegate.IsInDesignerView)
            {
                await GetExternalLoginProviders();
            }

            InfoButton.TouchUpInside += InfoButton_TouchUpInside;
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            NavigationController.SetNavigationBarHidden(true, true);
        }

        private async Task GetExternalLoginProviders()
        {
            var bounds = UIScreen.MainScreen.Bounds;

            // show the loading overlay on the UI thread using the correct orientation sizing
            loadPop = new LoadingOverlay(bounds);
            View.Add(loadPop);

            string fullUrl = "/api/Account/ExternalLogins?returnUrl=/callback&generateState=true";
            ServerResponse<ExternalLogin[]> res = await ServerUtils.Get<ExternalLogin[]>(fullUrl, false);

            loadPop.Hide();

            if (res.Success)
            {
                providers = res.Data;

                for (int i = 0; i < providers.Length; i++)
                {
                    UIButton button = new UIButton();
                    nfloat buttonWidth = PromptLabel.Frame.Width;
                    nfloat buttonYPos = PromptLabel.Frame.GetMaxY() + 20f + (60 * i);
                    button.Frame = new CGRect(0f, buttonYPos, buttonWidth, 60f);
                    button.SetTitle(providers[i].Name, UIControlState.Normal);
                    button.TitleLabel.Font = UIFont.SystemFontOfSize(19);

                    button.TouchUpInside += (sender, e) =>
                    {
                        notificationToken = NSNotificationCenter.DefaultCenter.AddObserver(new NSString("ParkLearnLoginCallback"), OnLoginResult);

                        // Use the button's title to find the appropriate provider
                        ExternalLogin thisLogin = providers.FirstOrDefault(p => p.Name == ((UIButton)sender).Title(UIControlState.Normal));

                        if (thisLogin == null)
                        {
                            Console.WriteLine("Provider button is null");
                            return;
                        }

                        LoginAfterTerms(thisLogin.Url);
                    };

                    PromptLabel.Superview.AddSubview(button);
                }
            }
            else
            {
                // Show error and retry button
                var errorAlert = UIAlertController.Create("Connection Error", "Couldn't load login info from the server. Please try again.", UIAlertControllerStyle.Alert);
                errorAlert.AddAction(UIAlertAction.Create("Retry", UIAlertActionStyle.Default, (a) => { var suppressAsync = GetExternalLoginProviders(); }));
                PresentViewController(errorAlert, true, null);
            }
        }

        private void LoginAfterTerms(string loginUri)
        {
            var alertController = UIAlertController.Create("Terms & Privacy Policy",
                                                           "By continuing you confirm that you have read and agree to OurPlace's Terms & Conditions and Privacy Policy",
                                                           UIAlertControllerStyle.Alert);

            alertController.AddAction(UIAlertAction.Create("Read Terms & Conditions", UIAlertActionStyle.Default, (obj) =>
            {
                UIApplication.SharedApplication.OpenUrl(new NSUrl(ConfidentialData.api + "terms"));
            }));

            alertController.AddAction(UIAlertAction.Create("Read Privacy Policy", UIAlertActionStyle.Default, (obj) =>
            {
                UIApplication.SharedApplication.OpenUrl(new NSUrl(ConfidentialData.api + "privacypolicy"));
            }));

            alertController.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, (obj) => { }));

            alertController.AddAction(UIAlertAction.Create("Agree", UIAlertActionStyle.Default, (obj) =>
            {
                safariController = new SFSafariViewController(new NSUrl(ConfidentialData.api + loginUri));
                PresentViewControllerAsync(safariController, true);
            }));

            PresentViewController(alertController, true, null);
        }

        private void InfoButton_TouchUpInside(object sender, EventArgs e)
        {
            SignInExplanationController explanationController = Storyboard.InstantiateViewController(
                "SignInExplanationController") as SignInExplanationController;
            var navController = new UINavigationController(explanationController);

            explanationController.NavigationItem.LeftBarButtonItem = new UIBarButtonItem("Close", UIBarButtonItemStyle.Plain, delegate
            {
                explanationController.DismissViewController(true, null);
            });

            explanationController.NavigationItem.Title = "Logging into OurPlace";

            PresentViewControllerAsync(navController, true);
        }

        private async void OnLoginResult(NSNotification notification)
        {
            try
            {
                var bounds = UIScreen.MainScreen.Bounds;
                loadPop = new LoadingOverlay(bounds);
                View.Add(loadPop);

                // Login return format:
                // parklearn://logincallback?access_token=TOKEN&token_type=bearer&expires_in=7257600&state=STATE"

                NSObject returned = notification.UserInfo?[new NSString("url")];
                if (returned == null)
                {
                    throw new Exception("Error retrieving data from server");
                }

                await safariController.DismissViewControllerAsync(true);
                NSNotificationCenter.DefaultCenter.RemoveObserver(notificationToken);

                Uri uri = new Uri(returned.ToString());

                accessToken = Common.Helpers.GetUrlParam(uri, "access_token");
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    throw new Exception("No access token returned");
                }

                refreshToken = Common.Helpers.GetUrlParam(uri, "refresh_token");
                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    throw new Exception("No refresh token returned");
                }

                string expiresInSecs = Common.Helpers.GetUrlParam(uri, "expires_in");
                if (string.IsNullOrWhiteSpace(expiresInSecs))
                {
                    throw new Exception("No access token expiry information");
                }
                accessExpiresAt = DateTime.UtcNow.AddSeconds(int.Parse(expiresInSecs));

                string refreshExpiry = Common.Helpers.GetUrlParam(uri, "refresh_token_expires");
                if (string.IsNullOrWhiteSpace(refreshExpiry))
                {
                    throw new Exception("No refresh token expiry information");
                }
                refreshExpiresAt = new DateTime(long.Parse(refreshExpiry), DateTimeKind.Utc);

                ApplicationUser tempUser = new ApplicationUser()
                {
                    AccessToken = accessToken,
                    AccessExpiresAt = accessExpiresAt,
                    RefreshToken = refreshToken,
                    RefreshExpiresAt = refreshExpiresAt
                };

                (await Storage.GetDatabaseManager()).AddUser(tempUser);

                ServerResponse<ApplicationUser> res = await ServerUtils.Get<ApplicationUser>("api/account/");

                if (res != null && res.Success)
                {
                    ApplicationUser updatedUser = res.Data;
                    updatedUser.AccessToken = tempUser.AccessToken;
                    updatedUser.AccessExpiresAt = tempUser.AccessExpiresAt;
                    updatedUser.RefreshToken = tempUser.RefreshToken;
                    updatedUser.RefreshExpiresAt = tempUser.RefreshExpiresAt;

                    (await Storage.GetDatabaseManager()).AddUser(updatedUser);

                    loadPop.Hide();

                    NavigationController.SetNavigationBarHidden(false, true);

                    var storyBoard = UIStoryboard.FromName("Main", null);
                    var mainScreen = storyBoard.InstantiateViewController("MainTabController");

                    UIApplication.SharedApplication.KeyWindow.RootViewController = new UINavigationController(mainScreen);
                    NavigationController.PopToRootViewController(true);
                }
                else
                {
                    throw new Exception("Login failed");
                }
            }
            catch (Exception e)
            {
                loadPop.Hide();

                var errorAlert = UIAlertController.Create("Login Error", e.Message, UIAlertControllerStyle.Alert);
                PresentViewController(errorAlert, true, null);

                Console.WriteLine(e.Message);
            }
        }

    }
}