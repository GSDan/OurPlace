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
using FFImageLoading;
using Foundation;
using Newtonsoft.Json;
using OurPlace.Common.Models;
using UIKit;

namespace OurPlace.iOS
{
    public partial class TaskCell_Info : UITableViewCell
    {
        public static readonly NSString Key = new NSString("TaskCell_Info");
        public static readonly UINib Nib;
		private NSLayoutConstraint showImageConstraint;
        private NSLayoutConstraint hideImageConstraint;
        private AppTask taskData;
        private AdditionalInfoData info;

        static TaskCell_Info()
        {
            Nib = UINib.FromName("TaskCell_Info", NSBundle.MainBundle);
        }

        protected TaskCell_Info(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

		public void UpdateContent(AppTask data)
        {
            taskData = data;
            TaskDescription.Text = data.Description;
            info = JsonConvert.DeserializeObject<AdditionalInfoData>(data.JsonData);

            if (!string.IsNullOrWhiteSpace(info.ImageUrl))
            {
                string imgUrl = Common.ServerUtils.GetUploadUrl(info.ImageUrl);
                ImageService.Instance.LoadUrl(imgUrl).Into(InfoImage);
                NSLayoutConstraint.DeactivateConstraints(new NSLayoutConstraint[] { hideImageConstraint });
                NSLayoutConstraint.ActivateConstraints(new NSLayoutConstraint[] { showImageConstraint });
            }
            else
            {
                NSLayoutConstraint.DeactivateConstraints(new NSLayoutConstraint[] { showImageConstraint });
                NSLayoutConstraint.ActivateConstraints(new NSLayoutConstraint[] { hideImageConstraint });
            }

            if (!string.IsNullOrWhiteSpace(info.ExternalUrl))
            {
                InfoButton.Alpha = 1;
                InfoButton.TouchUpInside += (a, e) =>
                {
                    UIApplication.SharedApplication.OpenUrl(new NSUrl(info.ExternalUrl));
                };
            }
            else
            {
                InfoButton.Alpha = 0;
            }
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            showImageConstraint = NSLayoutConstraint.Create(InfoImage, NSLayoutAttribute.Height, NSLayoutRelation.Equal, 1, 200);
            showImageConstraint.Active = false;
            hideImageConstraint = NSLayoutConstraint.Create(InfoImage, NSLayoutAttribute.Height, NSLayoutRelation.Equal, 1, 0);
            hideImageConstraint.Active = true;

            NSLayoutConstraint.ActivateConstraints(new NSLayoutConstraint[] { hideImageConstraint });
        }
    }
}
