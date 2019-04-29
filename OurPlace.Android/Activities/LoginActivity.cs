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
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.CustomTabs;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using OurPlace.Common;
using OurPlace.Common.Models;
using System;
using System.Linq;

namespace OurPlace.Android.Activities
{
    [Activity(Label = "Sign In", LaunchMode = global::Android.Content.PM.LaunchMode.SingleInstance)]
    public class LoginActivity : AppCompatActivity
    {
        private CustomTabsActivityManager chromeManager;
        private ExternalLogin[] providers;
        private LinearLayout buttonLayout;
        private CallbackBroadcastReceiver receiver;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.LoginActivity);

            buttonLayout = FindViewById<LinearLayout>(Resource.Id.buttonsLayout);

            TextView loginInfoPrompt = FindViewById<TextView>(Resource.Id.loginInfoPrompt);
            loginInfoPrompt.Click += LoginInfoPrompt_Click;

            receiver = new CallbackBroadcastReceiver(OnBroadcastReceived);

            GetExternalLoginProviders();
        }

        protected override void OnResume()
        {
            base.OnResume();
            RegisterReceiver(receiver, new IntentFilter("com.park.learn.CALLBACK"));
        }

        private void LoginInfoPrompt_Click(object sender, EventArgs e)
        {
            Intent moreInfoActivity = new Intent(this, typeof(LoginInfoActivity));
            StartActivity(moreInfoActivity);
        }

        private void LoginActivity_Click(object sender, EventArgs e)
        {
            View dialogLayout = LayoutInflater.Inflate(Resource.Layout.DialogButtonsX2, null);
            var termsButton = dialogLayout.FindViewById<Button>(Resource.Id.dialogBtn1);
            termsButton.Text = Resources.GetString(Resource.String.LoginOpenTerms);
            termsButton.Click += (args, o) =>
            {

                Intent browserIntent =
                        new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(
                            ConfidentialData.api + "terms"));
                StartActivity(browserIntent);
            };

            var privacyButton = dialogLayout.FindViewById<Button>(Resource.Id.dialogBtn2);
            privacyButton.Text = Resources.GetString(Resource.String.LoginOpenPrivacy);
            privacyButton.Click += (args, o) =>
            {

                Intent browserIntent =
                        new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(
                            ConfidentialData.api + "privacypolicy"));
                StartActivity(browserIntent);
            };

            new global::Android.Support.V7.App.AlertDialog.Builder(this)
                 .SetTitle(Resource.String.LoginTermsTitle)
                .SetMessage(Resource.String.LoginTerms)
                .SetView(dialogLayout)
                .SetNegativeButton(Resource.String.dialog_cancel, (a, args) => { })
                .SetPositiveButton(Resource.String.LoginAgreeTerms, (a, args) =>
                {
                    string buttonText = ((Button)sender).Text;

                    ExternalLogin chosen = providers.FirstOrDefault(prov => prov.Name == buttonText);

                    if (chosen == null)
                    {
                        return;
                    }

                    chromeManager = new CustomTabsActivityManager(this);

                    // build custom tab
                    var builder = new CustomTabsIntent.Builder(chromeManager.Session)
                        .SetShowTitle(true)
                        .EnableUrlBarHiding();

                    var customTabsIntent = builder.Build();
                    customTabsIntent.Intent.AddFlags(ActivityFlags.NoHistory);
                    chromeManager.Warmup(0L);
                    customTabsIntent.LaunchUrl(this, global::Android.Net.Uri.Parse(ConfidentialData.api + chosen.Url));
                })
                .Show();
        }

        protected override void OnDestroy()
        {
            chromeManager = null;
            UnregisterReceiver(receiver);
            base.OnDestroy();
        }

        private void OnBroadcastReceived()
        {
            Console.WriteLine("FINISHING LOGIN ACTIVITY");
            Finish();
        }

        private async void GetExternalLoginProviders()
        {
            ProgressDialog dialog = new ProgressDialog(this);
            dialog.SetTitle(Resources.GetString(Resource.String.Connecting));
            dialog.SetMessage(Resources.GetString(Resource.String.PleaseWait));
            dialog.SetCancelable(false);
            dialog.Show();

            const string fullUrl = "/api/Account/ExternalLogins?returnUrl=/callback&generateState=true";
            ServerResponse<ExternalLogin[]> res = await ServerUtils.Get<ExternalLogin[]>(fullUrl, false);

            dialog.Dismiss();

            if (res != null && res.Success)
            {
                providers = res.Data;

                if (providers == null || providers.Length <= 0)
                {
                    return;
                }

                LinearLayout.LayoutParams layoutParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent);
                layoutParams.SetMargins(5, 15, 5, 15);

                foreach (ExternalLogin ext in providers)
                {
                    Button thisButton = new Button(this) { Text = ext.Name };
                    thisButton.SetBackgroundResource(Resource.Color.app_purple);
                    thisButton.SetTextColor(Color.White);
                    thisButton.LayoutParameters = layoutParams;
                    thisButton.Click += LoginActivity_Click;
                    buttonLayout.AddView(thisButton);
                }
            }
            else
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.ErrorTitle)
                    .SetMessage(res?.Message)
                    .Show();
            }
        }
    }
}