﻿#region copyright
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
using System.Collections.Generic;

namespace OurPlace.Common.Models
{
    public class AppTask : LearningTask
    {
        public AppTask()
        {

        }

        public AppTask(LearningTask orig, AppTask parent = null)
        {
            Id = orig.Id;
            ChildTasks = orig.ChildTasks;
            TaskType = orig.TaskType;
            JsonData = orig.JsonData;
            Description = orig.Description;
            Order = orig.Order;
            IsChild = parent != null;
            CompletionData = new CompletedTask();

            ChildAppTasks = new List<AppTask>();

            if (orig.ChildTasks != null)
            {
                foreach (LearningTask child in orig.ChildTasks)
                {
                    if (child.JsonData != null && child.JsonData.StartsWith("TASK::"))
                    {
                        child.JsonData = "TASK::" + Id.ToString();
                    }
                    ChildAppTasks.Add(new AppTask(child, this));
                }
            }
        }

        public bool IsCompleted { get; set; }
        public CompletedTask CompletionData { get; set; }
        public bool IsAvailable;
        public List<AppTask> ChildAppTasks { get; set; }
        public bool IsChild { get; set; }
    }
}
