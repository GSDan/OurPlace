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
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OurPlace.Android.Adapters
{
    public class TaskTypeAdapter : RecyclerView.Adapter
    {
        public event EventHandler<int> ItemClick;
        public List<TaskType> Data;
        public Context Context;

        public TaskTypeAdapter(Context context, List<TaskType> data)
        {
            Data = data;
            Context = context;

            Data = Data.OrderBy(tt => tt.Order).ToList();
        }

        public override int ItemCount => Data?.Count ?? 0;

        public override int GetItemViewType(int position)
        {
            if (position == 0)
            {
                return 0;
            }

            return position >= Data.Count ? 2 : 1;
        }

        private void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (!(holder is TaskViewHolderBtn vh))
            {
                return;
            }

            vh.Title.Text = Data[position].DisplayName;
            vh.Description.Text = Data[position].Description;
            AndroidUtils.LoadTaskTypeIcon(Data[position], vh.TaskTypeIcon);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Btn, parent, false);
            TaskViewHolderBtn vh = new TaskViewHolderBtn(itemView, null, OnClick)
            {
                Button = { Text = Context.Resources.GetString(Resource.String.ChooseBtn) }
            };
            return vh;
        }
    }
}