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
using Android.Support.V4.App;
using Java.Lang;
using OurPlace.Android.Fragments;

namespace OurPlace.Android.Adapters
{
    public class PagerAdapter : FragmentPagerAdapter
    {
        private readonly Context context;

        public PagerAdapter(FragmentManager fm, Context context) : base(fm)
        {
            this.context = context;
        }

        public override int Count => 2;

        public override Fragment GetItem(int position)
        {
            switch (position)
            {
                case 0:
                    return new MainLandingFragment();
                case 1:
                    return new MainMyActivitiesFragment();
                default:
                    return null;
            }
        }

        public override ICharSequence GetPageTitleFormatted(int position)
        {
            switch(position)
            {
                case 0:
                    return new String(context.Resources.GetString(Resource.String.MainLandingTabTitle));
                case 1:
                    return new String(context.Resources.GetString(Resource.String.MainMyActivitiesTabTitle));
                default:
                    return new String("ERROR");
            }
        }
    }
}