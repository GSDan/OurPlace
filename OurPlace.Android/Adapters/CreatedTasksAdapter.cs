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
    public class CreatedTasksAdapter : RecyclerView.Adapter, ItemTouchHelperAdapter
    {
        public event EventHandler<int> EditActivityClick;
        public event EventHandler<int> FinishClick;
        public event EventHandler<int> EditItemClick;
        public event EventHandler<int> DeleteItemClick;
        public event EventHandler<int> ManageChildrenItemClick;

        public Action SaveProgress;
        public List<LearningTask> Data;
        private readonly Context context;
        private LearningActivity learningActivity;
        private bool editingSubmitted;

        public CreatedTasksAdapter(Context context, LearningActivity learningAct, bool editingSubmitted, Action save)
        {
            this.editingSubmitted = editingSubmitted;

            if (learningAct.LearningTasks != null)
            {
                Data = (List<LearningTask>)learningAct.LearningTasks;
            }
            else
            {
                Data = new List<LearningTask>();
            }

            this.context = context;
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
                if (Data == null)
                {
                    return 2;
                }

                return Data.Count + 2;
            }
        }

        public override int GetItemViewType(int position)
        {
            if (position == 0)
            {
                return 0;
            }

            return position > Data.Count ? 2 : 1;
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
            if (Data[position] == null || Data[position].TaskType == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(Data[position].TaskType.IconUrl))
            {
                ImageService.Instance.LoadCompiledResource("OurPlace_logo")
                    .Into(view);
            }
            else
            {
                ImageService.Instance.LoadUrl(Data[position].TaskType.IconUrl)
                    .Transform(new CircleTransformation())
                    .Into(view);
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (position == 0)
            {
                if (!(holder is ActivityViewHolder avh))
                {
                    return;
                }

                avh.Title.Text = learningActivity.Name;
                avh.Description.Text = learningActivity.Description;

                if (string.IsNullOrWhiteSpace(learningActivity.ImageUrl)) return;

                if (learningActivity.ImageUrl.StartsWith("upload"))
                {
                    ImageService.Instance.LoadUrl(ServerUtils.GetUploadUrl(learningActivity.ImageUrl))
                        .Transform(new CircleTransformation())
                        .Into(avh.TaskTypeIcon);
                }
                else
                {
                    ImageService.Instance.LoadFile(learningActivity.ImageUrl)
                        .Transform(new CircleTransformation())
                        .Into(avh.TaskTypeIcon);
                }

                return;
            }

            if (position > Data.Count)
            {
                // Allow the activity to be submitted if there's at least one task
                ButtonViewHolder bvh = holder as ButtonViewHolder;
                bvh.Button.Enabled = Data.Count > 0;
                return;
            }

            position--;

            if (!(holder is TaskViewHolderCreatedTask vh))
            {
                return;
            }

            vh.Title.Text = Data[position].TaskType.DisplayName;
            vh.Description.Text = Data[position].Description;

            if (((List<LearningTask>) Data[position].ChildTasks).Count > 0)
            {
                vh.ManageChildrenBtn.Text = context.GetString(Resource.String.createTaskChildrenManage);
            }
            else
            {
                vh.ManageChildrenBtn.Text = context.GetString(Resource.String.createTaskChildrenAdd);
            }

            vh.DeleteBtn.Text = context.Resources.GetString(Resource.String.RemoveBtn);
            LoadImage(position, vh.TaskTypeIcon);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            switch (viewType)
            {
                case 0:
                {
                    // The Activity details at the top of the list
                    View activityView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Activity, parent, false);
                    ActivityViewHolder avh = new ActivityViewHolder(activityView, null, OnEditActivityClick);
                    return avh;
                }
                case 2:
                {
                    // The finish button at the bottom of the list
                    View finishView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Finish, parent, false);
                    ButtonViewHolder bvh = new ButtonViewHolder(finishView, OnFinishClick);
                    return bvh;
                }
            }

            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.CreateTaskCard, parent, false);
            TaskViewHolderCreatedTask vh = new TaskViewHolderCreatedTask(itemView, null, OnDeleteItemClick, OnEditItemClick, OnManageChildrenClick);
            return vh;
        }

        public bool onItemMove(int fromPosition, int toPosition)
        {
            // Account for the header and finish cards
            int dataFrom = fromPosition - 1;

            if (dataFrom < 0 || dataFrom >= Data.Count)
            {
                return false;
            }

            int dataTo = Math.Min(toPosition - 1, Data.Count - 1);
            dataTo = Math.Max(dataTo, 0);

            Data.Swap(dataFrom, dataTo);
            NotifyItemMoved(fromPosition, toPosition);

            SaveProgress?.Invoke();

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

            if (position >= Data.Count)
            {
                return -1;
            }

            return Data[position].Id;
        }
    }
}