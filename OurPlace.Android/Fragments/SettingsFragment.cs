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
using Android.OS;
using Android.Preferences;
using Android.Widget;
using OurPlace.Common.LocalData;
using System.Threading.Tasks;
using static OurPlace.Common.LocalData.Storage;

namespace OurPlace.Android.Fragments
{
    public class SettingsFragment : PreferenceFragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AddPreferencesFromResource(Resource.Xml.preferences);

            FindPreference("pref_cacheDelete").PreferenceClick += ClearCache_PreferenceClick;
            FindPreference("pref_accountLogout").PreferenceClick += LogOut_PreferenceClick;
        }

        private void LogOut_PreferenceClick(object sender, Preference.PreferenceClickEventArgs e)
        {
            new AlertDialog.Builder(Activity)
                    .SetTitle(Resource.String.WarningTitle)
                    .SetMessage(Resource.String.pref_accountLogout_warning)
                    .SetPositiveButton(Resource.String.Continue, (a, b) => { LogOut(); })
                    .SetNegativeButton(Resource.String.dialog_cancel, (a, b) => { })
                    .SetCancelable(true)
                    .Show();
        }

        private void ClearCache_PreferenceClick(object sender, Preference.PreferenceClickEventArgs e)
        {
            new AlertDialog.Builder(Activity)
                    .SetTitle(Resource.String.WarningTitle)
                    .SetMessage(Resource.String.pref_cacheDelete_warning)
                    .SetPositiveButton(Resource.String.Continue, (a, b) => { var suppress = DeleteCache(); })
                    .SetNegativeButton(Resource.String.dialog_cancel, (a, b) => { })
                    .SetCancelable(true)
                    .Show();
        }

        private async Task DeleteCache()
        {
            DatabaseManager dbManager = await GetDatabaseManager();
            dbManager.DeleteLearnerCacheAndProgress();

            MainLandingFragment.ForceRefresh = true;

            Toast.MakeText(Activity, Resource.String.pref_cacheDelete_complete, ToastLength.Short).Show();
        }

        private void LogOut()
        {
            var suppress = AndroidUtils.ReturnToSignIn(Activity);
        }
    }
}