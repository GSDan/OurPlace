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
using Android.Views;
using Android.Widget;
using System;

namespace OurPlace.Android.Misc
{
    public class AlwaysVisibleMediaController : MediaController
    {
        private Action onBackPress;

        public AlwaysVisibleMediaController(Context context, Action onBackPressed) : base(context)
        {
            onBackPress = onBackPressed;
        }

        // Don't hide the controls
        public override void Show(int timeout)
        {
            try
            {
                base.Show(0);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        // Override back button, otherwise gets absorbed to hide the interface
        public override bool DispatchKeyEvent(KeyEvent e)
        {
            if (e.KeyCode == Keycode.Back)
            {
                onBackPress.Invoke();
                return true;
            }
            return base.DispatchKeyEvent(e);
        }
    }
}