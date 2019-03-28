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
using Android.Content;
using Android.Support.V7.Widget;
using Android.Views;
using FFImageLoading;
using FFImageLoading.Transformations;
using FFImageLoading.Views;
using OurPlace.Common;
using OurPlace.Common.Models;

namespace OurPlace.Android.Adapters
{
    public class CreatedChildTasksAdapter : RecyclerView.Adapter, ItemTouchHelperAdapter
    {
        public event EventHandler<int> FinishClick;
        public event EventHandler<int> EditItemClick;
        public event EventHandler<int> DeleteItemClick;

        public List<LearningTask> data;
        private readonly Context context;
        private LearningTask parentTask;

        public CreatedChildTasksAdapter(Context context, LearningTask parentTask)
        {
            if (parentTask.ChildTasks != null)
            {
                data = (List<LearningTask>)parentTask.ChildTasks;
            }
            else
            {
                data = new List<LearningTask>();
            }

            this.context = context;
            this.parentTask = parentTask;
        }

        public override int ItemCount
        {
            get
            {
                if (data == null)
                {
                    return 2;
                }

                return data.Count + 2;
            }
        }

        public override int GetItemViewType(int position)
        {
            if (position == 0)
            {
                return 0;
            }

            return position > data.Count ? 2 : 1;
        }

        private void OnEditItemClick(int position)
        {
            EditItemClick?.Invoke(this, position);
        }

        private void OnDeleteItemClick(int position)
        {
            DeleteItemClick?.Invoke(this, position);
        }

        private void OnFinishClick(int position)
        {
            FinishClick?.Invoke(this, position);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (position == 0)
            {
                ActivityViewHolder avh = holder as ActivityViewHolder;
                avh.Title.SetText(Resource.String.createTaskChildrenManage);
                avh.Button.Visibility = ViewStates.Gone;
                avh.Description.Text = string.Format(context.Resources.GetString(Resource.String.createTaskChildrenDesc),
                    parentTask.TaskType.DisplayName);

                AndroidUtils.LoadTaskTypeIcon(parentTask.TaskType, avh.TaskTypeIcon);
                return;
            }

            if (position > data.Count)
            {
                // Allow the activity to be submitted if there's at least one task
                if (!(holder is ButtonViewHolder bvh))
                {
                    return;
                }

                bvh.Button.Enabled = true;
                bvh.Button.SetText(Resource.String.createTaskChildrenFinish);

                return;
            }

            position--;

            if (!(holder is TaskViewHolderCreatedTask vh))
            {
                return;
            }

            vh.Title.Text = data[position].TaskType.DisplayName;
            vh.Description.Text = data[position].Description;
            vh.ManageChildrenBtn.Visibility = ViewStates.Gone;

            vh.DeleteBtn.Text = context.Resources.GetString(Resource.String.RemoveBtn);
            AndroidUtils.LoadTaskTypeIcon(data[position].TaskType, vh.TaskTypeIcon);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            switch (viewType)
            {
                case 0:
                {
                    // The Activity details at the top of the list
                    View activityView = LayoutInflater.From(parent.Context)
                        .Inflate(Resource.Layout.TaskCard_Activity, parent, false);
                    ActivityViewHolder avh = new ActivityViewHolder(activityView, null, null);
                    return avh;
                }
                case 2:
                {
                    // The finish button at the bottom of the list
                    View finishView = LayoutInflater.From(parent.Context)
                        .Inflate(Resource.Layout.TaskCard_Finish, parent, false);
                    ButtonViewHolder bvh = new ButtonViewHolder(finishView, OnFinishClick);
                    return bvh;
                }
            }

            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.CreateTaskCard, parent, false);
            TaskViewHolderCreatedTask vh = new TaskViewHolderCreatedTask(itemView, null, OnDeleteItemClick, OnEditItemClick, null);
            return vh;
        }

        public bool onItemMove(int fromPosition, int toPosition)
        {
            // Account for the header and finish cards
            int dataFrom = fromPosition - 1;

            if (dataFrom < 0 || dataFrom >= data.Count)
            {
                return false;
            }

            int dataTo = Math.Min(toPosition - 1, data.Count - 1);
            dataTo = Math.Max(dataTo, 0);

            data.Swap(dataFrom, dataTo);
            NotifyItemMoved(fromPosition, toPosition);

            return true;
        }

        public void onItemDismiss(int position)
        {
            throw new NotImplementedException();
        }

        public override long GetItemId(int position)
        {
            if (position == 0)
            {
                return -2;
            }

            if (position >= data.Count)
            {
                return -1;
            }

            return data[position].Id;
        }
    }
}