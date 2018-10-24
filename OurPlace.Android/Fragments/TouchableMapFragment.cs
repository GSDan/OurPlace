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
using Android.Gms.Maps;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;

namespace OurPlace.Android.Fragments
{
    // http://blog.ostebaronen.dk/2014/11/using-mapfragment-inside-scrollview.html
    /// <summary>
    /// Prioritises map touches over scrollview touches, for panning around in the map
    /// </summary>
    public class TouchableMapFragment : MapFragment
    {
        public event EventHandler TouchDown;
        public event EventHandler TouchUp;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var root = base.OnCreateView(inflater, container, savedInstanceState);
            var wrapper = new TouchableWrapper(Activity);
            wrapper.SetBackgroundColor(Resources.GetColor(global::Android.Resource.Color.Transparent));
            ((ViewGroup)root).AddView(wrapper,
              new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

            wrapper.TouchUp = () =>
            {
                if (TouchUp != null)
                {
                    TouchUp(this, EventArgs.Empty);
                }
            };
            wrapper.TouchDown = () =>
            {
                if (TouchDown != null)
                {
                    TouchDown(this, EventArgs.Empty);
                }
            };

            return root;
        }

        private class TouchableWrapper : FrameLayout
        {
            public Action TouchDown;
            public Action TouchUp;

            #region ctors
            protected TouchableWrapper(IntPtr javaReference, JniHandleOwnership transfer)
              : base(javaReference, transfer) { }
            public TouchableWrapper(Context context)
              : this(context, null) { }
            public TouchableWrapper(Context context, IAttributeSet attrs)
              : this(context, attrs, 0) { }
            public TouchableWrapper(Context context, IAttributeSet attrs, int defStyle)
              : base(context, attrs, defStyle) { }
            #endregion

            public override bool DispatchTouchEvent(MotionEvent e)
            {
                switch (e.Action)
                {
                    case MotionEventActions.Down:
                        if (TouchDown != null)
                        {
                            TouchDown();
                        }

                        break;
                    case MotionEventActions.Cancel:
                    case MotionEventActions.Up:
                        if (TouchUp != null)
                        {
                            TouchUp();
                        }

                        break;
                }

                return base.DispatchTouchEvent(e);
            }
        }
    }
}