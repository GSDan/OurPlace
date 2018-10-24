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
using OurPlace.Common.Models;

namespace OurPlace.Android.Adapters
{
    internal class ChapterAdapter : RecyclerView.Adapter
    {
        private Context context;
        public List<LearningChapter> data;
        //public event EventHandler<int> ItemClick;

        private const int HEADER = 0;
        private const int CHAPTER = 1;
        private const int TASK_MULTIPLECHOICE = 2;

        public override int ItemCount
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ChapterAdapter(Context context, List<LearningChapter> data)
        {
            this.context = context;
            this.data = data;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            throw new NotImplementedException();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            throw new NotImplementedException();
        }
    }
}