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
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OurPlace.Android.Adapters
{
    public class TaskTypeAdapter : RecyclerView.Adapter
    {
        public event EventHandler<int> ItemClick;
        public List<TaskType> data;
        public Context context;

        public TaskTypeAdapter(Context _context, List<TaskType> _data)
        {
            data = _data;
            context = _context;

            data = data.OrderBy(tt => tt.Order).ToList();
        }

        public override int ItemCount
        {
            get
            {
                if (data == null) return 0;
                return data.Count;
            }
        }

        public override int GetItemViewType(int position)
        {
            if (position == 0) return 0;
            if (position >= data.Count) return 2;
            return 1;
        }

        private void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            TaskViewHolder_Btn vh = holder as TaskViewHolder_Btn;
            vh.Title.Text = data[position].DisplayName;
            vh.Description.Text = data[position].Description;
            ImageService.Instance.LoadUrl(data[position].IconUrl)
                    .Into(vh.Image);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TaskCard_Btn, parent, false);
            TaskViewHolder_Btn vh = new TaskViewHolder_Btn(itemView, null, OnClick);
            vh.Button.Text = context.Resources.GetString(Resource.String.ChooseBtn);
            return vh;
        }
    }
}