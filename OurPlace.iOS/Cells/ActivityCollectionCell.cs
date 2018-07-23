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
using FFImageLoading;
using Foundation;
using UIKit;

namespace OurPlace.iOS.Cells
{
    public partial class ActivityCollectionCell : UICollectionViewCell
    {
        public static readonly NSString Key = new NSString("ActivityCollectionCell");
        public static readonly UINib Nib;

        static ActivityCollectionCell()
        {
            Nib = UINib.FromName("ActivityCollectionCell", NSBundle.MainBundle);
        }

        protected ActivityCollectionCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public void UpdateContent(string title, string description, string url)
        {
            ActivityIcon.Image = null;
            TitleLabel.Text = title;
            DescriptionLabel.Text = description;

            if (string.IsNullOrWhiteSpace(url))
            {
                ImageService.Instance.LoadCompiledResource("AppLogo").Into(ActivityIcon);
            }
            else
            {
                // check if it's a local file
                if(File.Exists(url))
                {
                    ImageService.Instance.LoadFile(url).Into(ActivityIcon);
                }
                else
                {
                    ImageService.Instance.LoadUrl(url).Into(ActivityIcon);
                }
            }
        }

    }
}
