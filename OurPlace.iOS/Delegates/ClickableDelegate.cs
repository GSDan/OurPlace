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
using System.Drawing;
using CoreGraphics;
using Foundation;
using UIKit;

namespace OurPlace.iOS.Delegates
{
    public class ClickableDelegate : UICollectionViewDelegate
    {
        private readonly Action<int> onClick;

        public ClickableDelegate(Action<int> onClick)
        {
            this.onClick = onClick;
        }

        public override void ItemSelected(UICollectionView collectionView, Foundation.NSIndexPath indexPath)
        {
			onClick(indexPath.Row);
        }

    }

    public class ClickableTableDelegate : UITableViewDelegate
    {
        private readonly Action<int> onClick;

        public ClickableTableDelegate(Action<int> onClick)
        {
            this.onClick = onClick;
        }

		public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
		{
            onClick(indexPath.Row);
		}
    }

	public class SectionedClickableDelegate : UICollectionViewDelegateFlowLayout
    {
        private readonly Action<int, int> onClick;
        
		public SectionedClickableDelegate(Action<int, int> onClick)
        {
            this.onClick = onClick;
        }

        public override void ItemSelected(UICollectionView collectionView, Foundation.NSIndexPath indexPath)
        {
            onClick(indexPath.Section, indexPath.Row);
        }

		public override CGSize GetSizeForItem(UICollectionView collectionView, UICollectionViewLayout layout, NSIndexPath indexPath)
		{
			CGRect screenBounds = UIScreen.MainScreen.Bounds;
			float columns = 1;
			float height = 315;

			if(UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad)
			{
				columns = 2;
				height = (float)screenBounds.Size.Width / 2.2f;
			}

			return new CGSize((float)screenBounds.Size.Width / columns, height);
		}
	}
}
