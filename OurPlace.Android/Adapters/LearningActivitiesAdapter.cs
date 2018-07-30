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
using Android.App;
using Android.Support.V7.Widget;
using Android.Views;
using FFImageLoading;
using FFImageLoading.Work;
using OurPlace.Common.Models;
using SectionedRecyclerview.Droid;
using System;
using System.Collections.Generic;
using OurPlace.Common;
using OurPlace.Common.LocalData;
using Java.Lang;

namespace OurPlace.Android.Adapters
{
    public class GridSpanner : GridLayoutManager.SpanSizeLookup
    {
        private SectionedRecyclerViewAdapter adapter;
        private int totalCols;

        public GridSpanner(SectionedRecyclerViewAdapter adapt, int totalCols)
        {
            adapter = adapt;
            this.totalCols = totalCols;
        }

        public override int GetSpanSize(int position)
        {
            if(adapter.IsHeader(position))
            {
                return totalCols;
            }
            return 1;
        }
    }

    public class LearningActivitiesAdapter : SectionedRecyclerViewAdapter
    {
        public List<ActivityFeedSection> data;
        public event EventHandler<int> ItemClick;
        private DatabaseManager dbManager;

        const int HEADER = -2;
        const int ITEM = -1;

        public LearningActivitiesAdapter(List<ActivityFeedSection> data, DatabaseManager dbManager) : base()
        {
            this.data = data;
            this.dbManager = dbManager;
        }

        public override int SectionCount
        {
            get
            {
                if(data != null) return data.Count;
                return 0;
            }
        }

        private void OnClick(int position)
        {
            if (ItemClick != null)
                ItemClick(this, position);
        }

        public LearningActivity GetItem(int position)
        {
            if (position < 0) return null;
            int checkedItems = 0;
            foreach(ActivityFeedSection section in data)
            {
                checkedItems++; //include header
                if(section.Activities.Count + checkedItems > position)
                {
                    return section.Activities[position - checkedItems];
                }
                checkedItems += section.Activities.Count;
            }
            return null;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == HEADER)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.GridHeader, parent, false);
                GridHeaderViewHolder vh = new GridHeaderViewHolder(itemView);
                return vh;
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.GridItem, parent, false);
                LearningActivityViewHolder vh = new LearningActivityViewHolder(itemView, OnClick);
                return vh;
            }
        }

        public override int GetItemViewType(int section, int relativePosition, int absolutePosition)
        {
            return base.GetItemViewType(section, relativePosition, absolutePosition);
        }

        public override int GetItemCount(int sectionInd)
        {
            if(data != null && 
                data.Count > sectionInd &&
                data[sectionInd].Activities != null)
            {
                return data[sectionInd].Activities.Count;
            }
            return 0;
        }

        public override void OnBindFooterViewHolder(Java.Lang.Object p0, int p1)
        {
            // Not needed
        }

        public override void OnBindHeaderViewHolder(Java.Lang.Object holder, int sectionInd, bool p2)
        {
            GridHeaderViewHolder vh = holder as GridHeaderViewHolder;
            vh.Title.Text = data[sectionInd].Title;
            vh.Description.Text = data[sectionInd].Description;
        }

        public override void OnBindViewHolder(Java.Lang.Object holder, int sectionInd, int relativePos, int absPos)
        {
            LearningActivityViewHolder vh = holder as LearningActivityViewHolder;
            TaskParameter imTask = null;

            LearningActivity thisAct = data[sectionInd].Activities[relativePos];

            if (string.IsNullOrWhiteSpace(thisAct.ImageUrl))
            {
                imTask = ImageService.Instance.LoadCompiledResource("logoRect");
            }
            else if (thisAct.ImageUrl.StartsWith("/data", StringComparison.Ordinal))
            {
                imTask = ImageService.Instance.LoadFile(thisAct.ImageUrl);
            }
            else
            {
                imTask = ImageService.Instance.LoadUrl(Common.ServerUtils.GetUploadUrl(thisAct.ImageUrl));
            }

            imTask.DownSampleInDip(width: 150);
            imTask.Into(vh.Image);

            vh.Name.Text = thisAct.Name;

            string truncatedDesc = Helpers.Truncate(thisAct.Description, 100);
            vh.Description.Text = truncatedDesc;

            bool thisAuthor = thisAct.Author?.Id == dbManager.currentUser.Id;

            if (thisAuthor && (thisAct.Approved || thisAct.IsPublic == false))
            {
                vh.StatusText.Visibility = ViewStates.Gone;
                vh.TickIcon.Visibility = ViewStates.Visible;
            }
            else if (thisAuthor)
            {
                vh.StatusText.Visibility = ViewStates.Visible;
                vh.StatusText.SetText(Resource.String.activityPending);
                vh.TickIcon.Visibility = ViewStates.Gone;
            }
            else
            {
                vh.StatusText.Visibility = ViewStates.Gone;
                vh.TickIcon.Visibility = ViewStates.Gone;
            }
        }
    }
}