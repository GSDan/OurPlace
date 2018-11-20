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
using OurPlace.iOS.Helpers;
using OurPlace.Common.Models;
using System.Web;
using System.Threading.Tasks;
using System.Collections.Generic;
using OurPlace.Common.LocalData;
using System.Linq;
using OurPlace.Common;

namespace OurPlace.iOS
{
    public partial class MainTabBarController : UITabBarController
    {

        public MainTabBarController(IntPtr handle) : base(handle)
        {

        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var suppressAsync = ServerUtils.RefreshTaskTypes();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            var suppressAsync = UpdateUploadsBadge(null);
        }

        public async Task UpdateUploadsBadge(int? num)
        {
            if (num == null)
            {
                List<AppDataUpload> uploads = (await Storage.GetDatabaseManager()).GetUploadQueue().ToList();
                num = uploads?.Count;
            }

            Console.WriteLine("Updating badge: " + num);

            if (num > 0)
            {
                TabBar.Items[2].BadgeValue = num.ToString();
            }
            else
            {
                TabBar.Items[2].BadgeValue = null;
            }
        }
    }
}