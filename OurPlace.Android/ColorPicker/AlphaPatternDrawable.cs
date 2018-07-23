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
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics.Drawables;
using Android.Graphics;

namespace ColorPicker
{
	public class AlphaPatternDrawable : Drawable
	{
		private int mRectangleSize = 10;

		private Paint mPaint = new Paint();
		private Paint mPaintWhite = new Paint();
		private Paint mPaintGray = new Paint();

		private int numRectanglesHorizontal;
		private int numRectanglesVertical;

		/**
	 * Bitmap in which the pattern will be cahched.
	 */
		private Bitmap	mBitmap;

		public AlphaPatternDrawable(int rectangleSize) {
			mRectangleSize = rectangleSize;

			//TODO : change as per native lib
			mPaintWhite.Color = Color.White;
			mPaintGray.Color = Color.Gray;
		}

		public override void Draw (Canvas canvas)
		{
			canvas.DrawBitmap(mBitmap, null, Bounds, mPaint); 
		}


		public override int Opacity {
			get {
				return 0;
			}
		}

		public override void SetAlpha (int alpha)
		{
			throw new NotImplementedException ();
		}

		public override void SetColorFilter (ColorFilter cf)
		{
			throw new NotSupportedException ("ColorFilter is not supported by this drawwable.");
		}

		protected override void OnBoundsChange (Rect bounds)
		{
			base.OnBoundsChange (bounds);

			int height = bounds.Height();
			int width = bounds.Width();

			numRectanglesHorizontal = (int) Math.Ceiling((double)(width / mRectangleSize));
			numRectanglesVertical = (int) Math.Ceiling((double)height / mRectangleSize);

			generatePatternBitmap();
		}



		/**
	 * This will generate a bitmap with the pattern
	 * as big as the rectangle we were allow to draw on.
	 * We do this to chache the bitmap so we don't need to
	 * recreate it each time draw() is called since it
	 * takes a few milliseconds.
	 */
		private void generatePatternBitmap(){

			if(Bounds.Width() <= 0 || Bounds.Height() <= 0){
				return;
			}

			mBitmap = Bitmap.CreateBitmap(Bounds.Width(),Bounds.Height(),Bitmap.Config.Argb8888);
			Canvas canvas = new Canvas(mBitmap);

			Rect r = new Rect();
			Boolean verticalStartWhite = true;
			for (int i = 0; i <= numRectanglesVertical; i++) {

				Boolean isWhite = verticalStartWhite;
				for (int j = 0; j <= numRectanglesHorizontal; j++) {

					r.Top = i * mRectangleSize;
					r.Left = j * mRectangleSize;
					r.Bottom = r.Top + mRectangleSize;
					r.Right = r.Left + mRectangleSize;

					canvas.DrawRect(r, isWhite ? mPaintWhite : mPaintGray);

					isWhite = !isWhite;
				}

				verticalStartWhite = !verticalStartWhite;

			}

		}
	}
}

