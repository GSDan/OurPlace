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
using OurPlace.Common.LocalData;
using System;
using System.Collections.Generic;

namespace OurPlace.Android.Adapters
{
    public class UploadsAdapter : RecyclerView.Adapter
    {
        public event EventHandler<int> UploadClick;
        public event EventHandler<int> DeleteClick;
        public List<AppDataUpload> Data;
        public Context Context;

        public UploadsAdapter(Context context, List<AppDataUpload> data)
        {
            Data = data;
            Context = context;
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

        private void OnUploadClick(int position)
        {
            UploadClick?.Invoke(this, position);
        }

        private void OnDeleteClick(int position)
        {
            DeleteClick?.Invoke(this, position);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (!(holder is TaskViewHolderUploadCard vh))
            {
                return;
            }

            vh.Title.Text = Data[position].Name;
            vh.Description.Text = Data[position].Description;
            ImageService.Instance.LoadFile(Data[position].ImageUrl)
                .Into(vh.TaskTypeIcon);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.UploadCard, parent, false);
            TaskViewHolderUploadCard vh = new TaskViewHolderUploadCard(itemView, null, OnUploadClick, OnDeleteClick);
            return vh;
        }
    }
}