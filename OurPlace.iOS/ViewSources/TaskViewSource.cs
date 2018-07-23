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
using System.Linq;
using Foundation;
using OurPlace.Common.Models;
using UIKit;

namespace OurPlace.iOS.ViewSources
{
    public class TaskViewSource : UITableViewSource
    {
        public List<AppTask> Rows { get; set; }
        public string enteredName = "";
        public Single FontSize { get; set; }
        public SizeF ImageViewSize { get; set; }
        private LearningActivity activity;
        private readonly Action<AppTask> startTask;
        private readonly Action<AppTask, string, int> resClicked;
        private readonly Action editName;

        public TaskViewSource(LearningActivity data, List<AppTask> taskProgress, Action<AppTask> startTaskAction, Action<AppTask, string, int> resultClicked, Action nameEdit) : base()
        {
            activity = data;
            Rows = taskProgress.OrderBy(task => task.Order).ToList();
            startTask = startTaskAction;
            resClicked = resultClicked;
            editName = nameEdit;
        }


        public override nint NumberOfSections(UITableView tableView)
        {
            return 1;
        }


        public AppTask GetWithId(int id)
        {
            return Rows.FirstOrDefault(t => t.Id == id);
        }


        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            UITableViewCell thisCell = null;

            int row = indexPath.Row;

            if (activity.RequireUsername)
            {
                if (row > 0)
                {
                    // compensate for index offset compared to row data
                    row--;
                }
                else
                {
                    // Enter name cell
                    TaskCell_NameEntry nameCell = (TaskCell_NameEntry)tableView.DequeueReusableCell(TaskCell_NameEntry.Key, indexPath);
                    nameCell.Tag = row;
                    nameCell.UpdateContent(enteredName, editName);
                    nameCell.SetNeedsUpdateConstraints();
                    nameCell.UpdateConstraintsIfNeeded();
                    return nameCell;
                }
            }

            if (Rows[row].TaskType.IdName == "INFO")
            {
                TaskCell_Info infoCell = (TaskCell_Info)tableView.DequeueReusableCell(TaskCell_Info.Key, indexPath);
                infoCell.Tag = row;
                infoCell.UpdateContent(Rows[row]);
                thisCell = infoCell;
            }
            else if (new string[] { "MATCH_PHOTO", "TAKE_PHOTO", "TAKE_VIDEO", "REC_AUDIO", "DRAW", "DRAW_PHOTO" }.Contains(Rows[row].TaskType.IdName))
            {
                ResultTaskCell resultCell = (ResultTaskCell)tableView.DequeueReusableCell(ResultTaskCell.Key, indexPath);
                resultCell.Tag = row;
                resultCell.UpdateContent(Rows[row], startTask, resClicked);
                thisCell = resultCell;
            }
            else
            {
                TaskCell_Simple cell = (TaskCell_Simple)tableView.DequeueReusableCell(TaskCell_Simple.Key, indexPath);
                cell.Tag = row;
                cell.UpdateContent(Rows[row], startTask);
                thisCell = cell;
            }

            if (thisCell != null)
            {
                thisCell.SetNeedsUpdateConstraints();
                thisCell.UpdateConstraintsIfNeeded();
            }

            return thisCell;
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            if (Rows == null) return 0;

            if (activity.RequireUsername)
            {
                return Rows.Count + 1;
            }

            return Rows.Count;
        }
    }
}
