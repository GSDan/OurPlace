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

namespace OurPlace.Common.Models
{
    public class LearningTask : Model
    {
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public string JsonData { get; set; }

        public TaskType TaskType { get; set; }
        public IEnumerable<LearningTask> ChildTasks { get; set; }

        public LearningTask()
        {
            Random rand = new Random();
            Id = rand.Next();
            ChildTasks = new List<LearningTask>();
        }
    }
}
