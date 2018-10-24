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
using Android.Util;
using Android.Views;
using System;

namespace OurPlace.Android
{
    public class AutoFitTextureView : TextureView
    {
        private int mRatioWidth = 0;
        private int mRatioHeight = 0;

        public AutoFitTextureView(Context context)
            : this(context, null)
        {

        }
        public AutoFitTextureView(Context context, IAttributeSet attrs)
            : this(context, attrs, 0)
        {

        }
        public AutoFitTextureView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {

        }

        public void SetAspectRatio(int width, int height)
        {
            if (width == 0 || height == 0)
            {
                throw new ArgumentException("Size cannot be negative.");
            }

            mRatioWidth = width;
            mRatioHeight = height;
            RequestLayout();
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            int width = MeasureSpec.GetSize(widthMeasureSpec);
            int height = MeasureSpec.GetSize(heightMeasureSpec);

            if (0 == mRatioWidth || 0 == mRatioHeight)
            {
                SetMeasuredDimension(width, height);
            }
            else {

				SetMeasuredDimension(width, (int)(width * mRatioHeight / (float)mRatioWidth));

                //if (width < (float)height * mRatioWidth / (float)mRatioHeight)
                //{
                //    SetMeasuredDimension(width, (int)(width * mRatioHeight / (float)mRatioWidth));
                //}
                //else {
                //    SetMeasuredDimension((int)(height * mRatioWidth / (float)mRatioHeight), height);
                //}
            }
        }
    }
}