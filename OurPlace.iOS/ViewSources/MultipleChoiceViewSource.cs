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
using Foundation;
using OurPlace.Common.LocalData;
using UIKit;

namespace OurPlace.iOS.ViewSources
{
    public class MultipleChoiceViewSource : UITableViewSource
    {
        public List<string> Rows { get; set; }
        private NSIndexPath lastSelection;

        public MultipleChoiceViewSource(List<string> data) : base()
        {
            Rows = data;
        }

        public void UpdateData(List<string> data)
        {
            Rows = data;
        }

        public override nint NumberOfSections(UITableView tableView)
        {
            return 1;
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            return Rows.Count;
        }

		public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
		{
            if (lastSelection != null)
            {
                tableView.CellAt(lastSelection).Accessory = UITableViewCellAccessory.None;
            }

            lastSelection = indexPath;
            tableView.CellAt(indexPath).Accessory = UITableViewCellAccessory.Checkmark;
		}

		public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            MultipleChoiceCell cell = (MultipleChoiceCell)tableView.DequeueReusableCell(MultipleChoiceCell.Key, indexPath);
            cell.Tag = indexPath.Row;
            cell.UpdateContent(Rows[indexPath.Row]);
            return cell;
        }

        public int GetSelection()
        {
            if (lastSelection != null) return lastSelection.Row;
            return -1;
        }

    }
}
