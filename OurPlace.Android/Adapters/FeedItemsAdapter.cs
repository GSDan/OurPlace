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

using Android.Support.V7.Widget;
using Android.Views;
using FFImageLoading;
using FFImageLoading.Work;
using OurPlace.Common;
using OurPlace.Common.LocalData;
using OurPlace.Common.Models;
using SectionedRecyclerview.Droid;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = Java.Lang.Object;

namespace OurPlace.Android.Adapters
{
    public class GridSpanner : GridLayoutManager.SpanSizeLookup
    {
        private readonly SectionedRecyclerViewAdapter adapter;
        private readonly int totalCols;

        public GridSpanner(SectionedRecyclerViewAdapter adapt, int totalCols)
        {
            adapter = adapt;
            this.totalCols = totalCols;
        }

        public override int GetSpanSize(int position)
        {
            return adapter.IsHeader(position) ? totalCols : 1;
        }
    }

    public class FeedItemsAdapter : SectionedRecyclerViewAdapter
    {
        public List<FeedSection> Data;
        public event EventHandler<int> ItemClick;
        private readonly DatabaseManager dbManager;

        private const int Activity = 1;
        private const int Collection = 2;
        private const int Header = -2;

        public FeedItemsAdapter(List<FeedSection> data, DatabaseManager dbManager)
        {
            Data = data;
            this.dbManager = dbManager;
        }

        public override int SectionCount => Data?.Count ?? 0;

        private void OnClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        public FeedItem GetItem(int position, bool includeHeaders = false)
        {
            if (position < 0)
            {
                return null;
            }

            int checkedItems = 0;
            foreach (FeedSection section in Data)
            {
                if(includeHeaders)
                {
                    checkedItems++; // section header
                }

                int collsInSection = section.Collections?.Count ?? 0;
                if (collsInSection + checkedItems > position)
                {
                    return section.Collections[position - checkedItems];
                }
                checkedItems += collsInSection;

                int actsInSection = section.Activities?.Count ?? 0;
                if (actsInSection + checkedItems > position)
                {
                    return section.Activities[position - checkedItems];
                }
                checkedItems += actsInSection;
            }
            return null;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == Header)
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.GridHeader, parent, false);
                GridHeaderViewHolder vh = new GridHeaderViewHolder(itemView);
                return vh;
            }
            else
            {
                View itemView = LayoutInflater.From(parent.Context).Inflate((viewType == Collection)? Resource.Layout.GridItemCollection : Resource.Layout.GridItem, parent, false);
                LearningActivityViewHolder vh = new LearningActivityViewHolder(itemView, OnClick);
                return vh;
            }
        }

        public override int GetItemViewType(int section, int relativePosition, int absolutePosition)
        {
            FeedItem item = null;

            FeedSection thisSection = Data[section];

            int collsInSection = thisSection.Collections?.Count ?? 0;
            int actsInSection = thisSection.Activities?.Count ?? 0;
            if (collsInSection > relativePosition)
            {
                item = thisSection.Collections[relativePosition];
            }
            else if ((actsInSection + collsInSection) > relativePosition)
            {
                item = thisSection.Activities[relativePosition - collsInSection];
            }

            if (item != null)
            {
                if (item is LearningActivity) return Activity;
                if (item is ActivityCollection) return Collection;
            }

            return base.GetItemViewType(section, relativePosition, absolutePosition);
        }

        public override int GetItemCount(int sectionInd)
        {
            if (Data != null &&
                Data.Count > sectionInd &&
                Data[sectionInd] != null)
            {
                return (Data[sectionInd].Collections?.Count ?? 0) + (Data[sectionInd].Activities?.Count ?? 0);
            }
            return 0;
        }

        public override void OnBindFooterViewHolder(Object p0, int p1)
        {
            // Not needed
        }

        public override void OnBindHeaderViewHolder(Object holder, int sectionInd, bool p2)
        {
            if (!(holder is GridHeaderViewHolder vh))
            {
                return;
            }

            vh.Title.Text = Data[sectionInd].Title;
            vh.Description.Text = Data[sectionInd].Description;
        }

        public override void OnBindViewHolder(Object holder, int sectionInd, int relativePos, int absPos)
        {
            LearningActivityViewHolder vh = holder as LearningActivityViewHolder;
            TaskParameter imTask;

            FeedItem thisItem; 
            
            if(Data[sectionInd].Collections?.Count > relativePos)
            {
                thisItem = Data[sectionInd].Collections.ElementAt(relativePos);
            }
            else if(Data[sectionInd].Activities?.Count > (relativePos - Data[sectionInd].Collections?.Count ?? 0))
            {
                thisItem = Data[sectionInd].Activities.ElementAt(relativePos - (Data[sectionInd].Collections?.Count ?? 0));
            }
            else
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(thisItem.ImageUrl))
            {
                imTask = ImageService.Instance.LoadCompiledResource("logoRect");
            }
            else if (thisItem.ImageUrl.StartsWith("/data", StringComparison.Ordinal))
            {
                imTask = ImageService.Instance.LoadFile(thisItem.ImageUrl);
            }
            else
            {
                imTask = ImageService.Instance.LoadUrl(ServerUtils.GetUploadUrl(thisItem.ImageUrl));
            }

            imTask.DownSampleInDip(width: 150);
            if (vh == null)
            {
                return;
            }

            imTask.Into(vh.Image);
            vh.Name.Text = thisItem.Name;

            string truncatedDesc = Helpers.Truncate(thisItem.Description, 100);
            vh.Description.Text = truncatedDesc;

            bool thisAuthor = thisItem.Author?.Id == dbManager.CurrentUser.Id;

            if (thisAuthor && (thisItem.Approved || thisItem.IsPublic == false))
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