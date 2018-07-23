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
using OurPlace.Android;

namespace ColorPicker
{
	public class ColorPickerDialog : AlertDialog,ColorPickerView.OnColorChangedListener
	{
		private ColorPickerView mColorPicker;

		private ColorPanelView mOldColor;
		private ColorPanelView mNewColor;
		 
		private ColorPickerView.OnColorChangedListener mListener;

		public ColorPickerDialog(Context context, int initialColor) : base(context,initialColor) {
			mListener = null;
			init(initialColor);
		}

		public ColorPickerDialog(Context context, int initialColor, ColorPickerView.OnColorChangedListener listener) : base(context) {
			mListener = listener;
			init(initialColor);
		}


		private void init(int color) {
			// To fight color branding.
			Window.SetFormat(Android.Graphics.Format.Rgb888);
			setUp(color);
		}

		private void setUp(int color) {
			Boolean isLandscapeLayout = false;

			LayoutInflater inflater = (LayoutInflater)Context.GetSystemService (Context.LayoutInflaterService);

			View layout = inflater.Inflate(Resource.Layout.dialog_color_picker, null);

			SetContentView(layout);

			SetTitle("Pick a Color");
			// setIcon(android.R.drawable.ic_dialog_info);

			LinearLayout landscapeLayout = (LinearLayout) layout.FindViewById(Resource.Id.dialog_color_picker_extra_layout_landscape);

			if(landscapeLayout != null) {
				isLandscapeLayout = true;
			}

			mColorPicker = (ColorPickerView) layout.FindViewById(Resource.Id.color_picker_view);
			mOldColor = (ColorPanelView) layout.FindViewById(Resource.Id.color_panel_old);
			mNewColor = (ColorPanelView) layout.FindViewById(Resource.Id.color_panel_new);

			if(!isLandscapeLayout) {
				((LinearLayout) mOldColor.Parent).SetPadding(
					(int)Math.Round(mColorPicker.getDrawingOffset()), 
					0, 
					(int)Math.Round(mColorPicker.getDrawingOffset()), 
					0);

			}
			else {
				landscapeLayout.SetPadding(0, 0,(int) Math.Round(mColorPicker.getDrawingOffset()), 0);
				string temp = null;
				SetTitle(temp);
			}

			mColorPicker.setOnColorChangedListener(this);

			mOldColor.setColor(color);
			mColorPicker.setColor(color, true);

		}
		 
		//TODO : change as per native lib for override
		 
		public void onColorChanged(int color) {
			mNewColor.setColor(color);

			if (mListener != null) {
				mListener.onColorChanged(color);

			}

		}

		public void setAlphaSliderVisible(Boolean visible) {
			mColorPicker.setAlphaSliderVisible(visible);
		}

		public int getColor() {
			return mColorPicker.getColor();
		}
	}
}

