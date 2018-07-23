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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Foundation;
using OurPlace.Common.Models;
using OurPlace.iOS.Cells;
using UIKit;

namespace OurPlace.iOS.ViewSources
{
    public class ActivityViewSource : UICollectionViewSource
    {
        public List<ActivityFeedSection> Rows { get; set; }
        public Single FontSize { get; set; }

        public ActivityViewSource() : base()
        {
            Rows = new List<ActivityFeedSection>();
        }

        public override nint NumberOfSections(UICollectionView collectionView)
        {
            if (Rows == null) return 0;
            return Rows.Count;
        }

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
        {
            if (Rows != null &&
               Rows.Count >= section &&
               Rows[(int)section] != null &&
               Rows[(int)section].Activities != null)
            {
                return Rows[(int)section].Activities.Count;
            }
            return 0;
        }

        public override UICollectionViewCell GetCell(UICollectionView collectionView, Foundation.NSIndexPath indexPath)
        {
            LearningActivity row = Rows[indexPath.Section]?.Activities[indexPath.Row];

            if (row == null) return null;

            var cell = (ActivityCollectionCell)collectionView.DequeueReusableCell(ActivityCollectionCell.Key, indexPath);


            string url = (!string.IsNullOrWhiteSpace(row.ImageUrl)) ? AppUtils.GetPathForLocalFile(row.ImageUrl) : "";

            if (!File.Exists(url))
            {
                url = (string.IsNullOrWhiteSpace(row.ImageUrl)) ?
                "" :
                Common.ServerUtils.GetUploadUrl(row.ImageUrl);
            }

            cell.UpdateContent(row.Name, row.Description, url);

            return cell;
        }

        public override UICollectionReusableView GetViewForSupplementaryElement(UICollectionView collectionView, NSString elementKind, NSIndexPath indexPath)
        {
            if (elementKind == UICollectionElementKindSectionKey.Header)
            {
                FeedSectionHeader headerView = (FeedSectionHeader)collectionView.DequeueReusableSupplementaryView(elementKind, FeedSectionHeader.Key, indexPath);
                headerView.UpdateContent(Rows[indexPath.Section].Title, Rows[indexPath.Section].Description);
                return headerView;
            }

            return base.GetViewForSupplementaryElement(collectionView, elementKind, indexPath);
        }
    }
}
