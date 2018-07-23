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
using OurPlace.Common.Models;
using UIKit;

namespace OurPlace.iOS.ViewSources
{
    public class TaskTypeViewSource : UITableViewSource
    {
        private readonly List<TaskType> Rows;
        private Action<TaskType> OnClick;

        public TaskTypeViewSource(List<TaskType> data, Action<TaskType> clicked)
        {
            Rows = data;
            OnClick = clicked;
        }

        public override nint NumberOfSections(UITableView tableView)
        {
            return 1;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            TaskTypeCell cell = (TaskTypeCell)tableView.DequeueReusableCell(TaskTypeCell.Key, indexPath);
            cell.UpdateContent(Rows[indexPath.Row], OnClick);
            return cell;
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            if (Rows == null) return 0;
            return Rows.Count;
        }
    }
}
