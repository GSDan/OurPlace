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
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using OurPlace.Common.Models;
using static OurPlace.Common.LocalData.Storage;

namespace OurPlace.Android.Activities
{
    [Activity(Label = "Login", NoHistory = true)]
    [IntentFilter(new[] { Intent.ActionView }, DataScheme = "parklearn", DataHost = "logincallback", Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault })]
    public class LoginResultActivity : Activity
    {
        private string tokenType;
        private string accessToken;
        private DateTime accessTokenExpiresAt;
        private string refreshToken;
        private DateTime refreshTokenExpiresAt;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            global::Android.Net.Uri dataUri = base.Intent.Data;

            if (dataUri == null) return;

            accessToken = dataUri.GetQueryParameter("access_token");
            tokenType = dataUri.GetQueryParameter("token_type");
            accessTokenExpiresAt = DateTime.Now.AddSeconds(int.Parse(dataUri.GetQueryParameter("expires_in")));
            refreshToken = dataUri.GetQueryParameter("refresh_token");
            refreshTokenExpiresAt = new DateTime(long.Parse(dataUri.GetQueryParameter("refresh_token_expires")), DateTimeKind.Utc);

            GetAccountDetails();
        }

        private async void GetAccountDetails()
        {
            ProgressDialog dialog = new ProgressDialog(this);
            dialog.SetTitle(Resources.GetString(Resource.String.Connecting));
            dialog.SetMessage(Resources.GetString(Resource.String.PleaseWait));
            dialog.Show();

            ApplicationUser tempUser = new ApplicationUser()
            {
                AccessToken = accessToken,
                AccessExpiresAt = accessTokenExpiresAt,
                RefreshToken = refreshToken,
                RefreshExpiresAt = refreshTokenExpiresAt
            };

            (await GetDatabaseManager()).AddUser(tempUser);

            Common.ServerResponse<ApplicationUser> res = await Common.ServerUtils.Get<ApplicationUser>("api/account/");

            dialog.Dismiss();

            if (res == null)
            {
                var suppress = AndroidUtils.ReturnToSignIn(this);
                Toast.MakeText(this, Resource.String.ForceSignOut, ToastLength.Long).Show();
                return;
            }

            if (res != null && res.Success)
            {
                // kill the login activity
                Intent message = new Intent("com.park.learn.CALLBACK");
                SendBroadcast(message);

                ApplicationUser updatedUser = res.Data;
                updatedUser.AccessToken = tempUser.AccessToken;
                updatedUser.AccessExpiresAt = tempUser.AccessExpiresAt;
                updatedUser.RefreshToken = tempUser.RefreshToken;
                updatedUser.RefreshExpiresAt = tempUser.RefreshExpiresAt;
               
                (await GetDatabaseManager()).AddUser(updatedUser);
                Intent intent = new Intent(this, typeof(MainActivity));
                intent.AddFlags(ActivityFlags.ClearTop);
                StartActivity(intent);
                Finish();
            }
            else
            {
                // show error message, return to login screen
                new AlertDialog.Builder(this)
                    .SetTitle(Resource.String.ErrorTitle)
                    .SetMessage(Resource.String.ErrorLogin)
                    .SetCancelable(false)
                    .SetOnDismissListener(new OnDismissListener(() => {
                        Intent intent = new Intent(this, typeof(LoginActivity));
                        StartActivity(intent);
                        Finish();
                    }))
                    .Show();
            }
        }
    }
}