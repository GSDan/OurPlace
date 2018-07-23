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
using Android.Content;
using Android.Support.V7.Widget;
using Android.Views;
using FFImageLoading;
using FFImageLoading.Transformations;
using FFImageLoading.Views;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using OurPlace.Common;

namespace OurPlace.Android.Adapters
{
    public class CreatedTasksAdapter : RecyclerView.Adapter, ItemTouchHelperAdapter
    {
        public event EventHandler<int> EditActivityClick;
        public event EventHandler<int> FinishClick;
        public event EventHandler<int> EditItemClick;
        public event EventHandler<int> DeleteItemClick;
        public event EventHandler<int> ManageChildrenItemClick;

        public Action SaveProgress;
        public List<LearningTask> data;
        private Context context;
        private LearningActivity learningActivity;

        public CreatedTasksAdapter(Context _context, LearningActivity learningAct, Action save)
        {
            if (learningAct.LearningTasks != null)
            {
                data = (List<LearningTask>)learningAct.LearningTasks;
            }
            else
            {
                data = new List<LearningTask>();
            }

            context = _context;
            learningActivity = learningAct;
            SaveProgress = save;
        }

        public void UpdateActivity(LearningActivity newAct)
        {
            learningActivity = newAct;
            NotifyItemChanged(0);
        }

        public override int ItemCount
        {
            get
            {
                if (data == null) return 2;
                return data.Count + 2;
            }
        }

        public override int GetItemViewType(int position)
        {
            if (position == 0) return 0;
            if (position > data.Count) return 2;
            return 1;
        }

        private void OnEditActivityClick(int position)
        {
            EditActivityClick?.Invoke(this, position);
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

        private void OnManageChildrenClick(int position)
        {
            ManageChildrenItemClick?.Invoke(this, position);
        }

        private void LoadImage(int position, ImageViewAsync view)
        {
            if (data[position] == null || data[position].TaskType == null) return;

            if (string.IsNullOrEmpty(data[position].TaskType.IconUrl))
            {
                ImageService.Instance.LoadCompiledResource("OurPlace_logo")
                    .Into(view);
            }
            else
            {
                ImageService.Instance.LoadUrl(data[position].TaskType.IconUrl)
                    .Transform(new CircleTransformation())
                    .Into(view);
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (position == 0)
            {
                ActivityViewHolder avh = holder as ActivityViewHolder;
                avh.Title.Text = learningActivity.Name;
                avh.Description.Text = learningActivity.Description;

                if (!string.IsNullOrWhiteSpace(learningActivity.ImageUrl))
                {
                    ImageService.Instance.LoadFile(learningActivity.ImageUrl)
                        .Transform(new CircleTransformation())
                        .Into(avh.Image);
                }
                return;
            }
            else if (position > data.Count)
            {
                // Allow the activity to be submitted if there's at least one task
                ButtonViewHolder bvh = holder as ButtonViewHolder;
                bvh.Button.Enabled = data.Count > 0;
                return;
            }

            position--;

            TaskViewHolder_CreatedTask vh = holder as TaskViewHolder_CreatedTask;
            vh.Title.Text = data[position].TaskType.DisplayName;
            vh.Description.Text = data[position].Description;

            if (((List<LearningTask>)data[position].ChildTasks).Count > 0)
            {
                vh.ManageChildrenBtn.Text = context.GetString(Resource.String.createTaskChildrenManage);
            }
            else
            {
                vh.ManageChildrenBtn.Text = context.GetString(Resource.String.createTaskChildrenAdd);
            }

            vh.DeleteBtn.Text = context.Resources.GetString(Resource.String.RemoveBtn);
            LoadImage(position, vh.Image);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == 0)
            {
                // The Activity details at the top of the list
                View activityView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Activity, parent, false);
                ActivityViewHolder avh = new ActivityViewHolder(activityView, null, OnEditActivityClick);
                return avh;
            }
            else if (viewType == 2)
            {
                // The finish button at the bottom of the list
                View finishView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Finish, parent, false);
                ButtonViewHolder bvh = new ButtonViewHolder(finishView, OnFinishClick);
                return bvh;
            }

            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.CreateTaskCard, parent, false);
            TaskViewHolder_CreatedTask vh = new TaskViewHolder_CreatedTask(itemView, null, OnDeleteItemClick, OnEditItemClick, OnManageChildrenClick);
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

            if(SaveProgress != null)
            {
                SaveProgress();
            }

            return true;
        }

        public void onItemDismiss(int position)
        {
            throw new NotImplementedException();
        }

        public override long GetItemId(int position)
        {
            if (position == 0) return -2;
            if (position >= data.Count) return -1;

            return data[position].Id;
        }
    }
}