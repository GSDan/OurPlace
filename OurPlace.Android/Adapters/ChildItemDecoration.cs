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
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;

namespace OurPlace.Android.Adapters
{
    public class ChildItemDecoration : RecyclerView.ItemDecoration
    {
        private readonly int margin;

        public ChildItemDecoration(Context context, int marginDips)
        {
            DisplayMetrics metrics = context.Resources.DisplayMetrics;
            margin = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, marginDips, metrics);
        }

        public override void GetItemOffsets(Rect outRect, View view, RecyclerView parent, RecyclerView.State state)
        {
            int itemPos = parent.GetChildAdapterPosition(view);

            TaskAdapter adapter = (TaskAdapter)parent.GetAdapter();

            if (!adapter.IsPositionAChildView(itemPos))
            {
                return;
            }

            outRect.Right = margin;
            outRect.Left = margin;
        }
    }
}