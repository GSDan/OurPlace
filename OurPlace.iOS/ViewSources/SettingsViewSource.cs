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
using System.Threading.Tasks;
using Foundation;
using OurPlace.Common;
using OurPlace.Common.LocalData;
using UIKit;

namespace OurPlace.iOS.ViewSources
{
    public class SettingsViewSource : UITableViewSource
    {
        private string[] options = { "Open OurPlace website", "How to use OurPlace", "Wipe local data", "Sign out" };
        private string identifier = "settingsCell";
        private UIViewController context;
        private Action<NSUrl> openWebPage;

        public SettingsViewSource(UIViewController controller, Action<NSUrl> openWebPageCallback)
        {
            openWebPage = openWebPageCallback;
            context = controller;
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            return options.Length;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            UITableViewCell cell = tableView.DequeueReusableCell(identifier);
            string thisOption = options[indexPath.Row];

            if(cell == null)
            {
                cell = new UITableViewCell(UITableViewCellStyle.Default, identifier);
            }

            cell.TextLabel.Text = thisOption;

            return cell;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            switch(indexPath.Row)
            {
                case 0:
                    // Open website
                    openWebPage?.Invoke(new NSUrl(ConfidentialData.api));
                    break;
                case 1:
                    // Tutorial
                    openWebPage?.Invoke(new NSUrl(ConfidentialData.api + "GettingStarted"));
                    break;
                case 2:
                    // Clear cache
                    AppUtils.ShowChoiceDialog(
                        context,
                        "Delete Local Data?",
                        "This will delete any downloaded files, as well as activity progress and pending uploads. " +
                        "\nAre you sure?",
                        "Delete",
                        (choice) => { var suppress = CleanCache(); },
                        "Cancel",
                        null, 1);
                    break;
                case 3:
                    // Sign out
                    AppUtils.ShowChoiceDialog(
                        context,
                        "Sign out of OurPlace?",
                        "This will delete any downloaded files, as well as activity progress and pending uploads. " +
                        "You will have to sign back in to use the app again. \nAre you sure?",
                        "Sign Out",
                        (choice) => { var suppress = AppUtils.SignOut(context); },
                        "Cancel",
                        null, 1);
                    break;
            }
        }

        private async Task CleanCache()
        {
            Console.WriteLine("Clean cache");
            Storage.CleanCache();

            await (context.ParentViewController as MainTabBarController).UpdateUploadsBadge(null);

            AppUtils.ShowSimpleDialog(context, "Finished!", "All local files have been deleted.", "Got it");
        }
    }
}
