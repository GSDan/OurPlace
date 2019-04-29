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

using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;

namespace OurPlace.Android.Adapters
{
    public interface ItemTouchHelperAdapter
    {
        bool onItemMove(int fromPosition, int toPosition);
        void onItemDismiss(int position);
    }

    public class DragHelper : ItemTouchHelper.Callback
    {
        private ItemTouchHelperAdapter touchHelper;

        public DragHelper(ItemTouchHelperAdapter dragAdapter)
        {
            touchHelper = dragAdapter;
        }

        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            int dragFlags = ItemTouchHelper.Up | ItemTouchHelper.Down;
            int swipeFlags = ItemTouchHelper.Start | ItemTouchHelper.End;

            // Only task cards should be movable
            return (viewHolder.ItemViewType == 1) ? MakeMovementFlags(dragFlags, swipeFlags) : 0;
        }

        public override bool OnMove(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder, RecyclerView.ViewHolder target)
        {
            if (viewHolder.ItemViewType != 1)
            {
                return false;
            }

            touchHelper.onItemMove(viewHolder.AdapterPosition, target.AdapterPosition);
            return true;
        }

        public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction)
        {
            touchHelper.onItemDismiss(viewHolder.AdapterPosition);
        }

        public override bool IsLongPressDragEnabled
        {
            get
            {
                return true;
            }
        }

        public override bool IsItemViewSwipeEnabled
        {
            get
            {
                // Drag only, no swipe
                return false;
            }
        }
    }
}