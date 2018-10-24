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
using Android.Preferences;
using Android.Graphics;
using Android.Util;
using Java.Lang;
using Android.Content.Res;
using ColorPicker;
using OurPlace.Android;

namespace colorpicker
{
	public class colorpickerpreference : DialogPreference,ColorPickerView.OnColorChangedListener
	{
		private ColorPickerView				mColorPickerView;
		private ColorPanelView				mOldColorView;
		private ColorPanelView				mNewColorView;

		private int							mColor;

		private System.Boolean				alphaChannelVisible = false;
		private string						alphaChannelText = null;
		private System.Boolean				showDialogTitle = false;
		private System.Boolean				showPreviewSelectedColorInList = true;
		private int							colorPickerSliderColor = -1;
		private int							colorPickerBorderColor = -1;


		public colorpickerpreference(Context context, IAttributeSet attrs) : base(context, attrs){
			init(attrs);
		}

		public colorpickerpreference(Context context, IAttributeSet attrs, int defStyle) :base(context, attrs, defStyle){
			init(attrs); 
		} 

		private void init(IAttributeSet attrs) {
			TypedArray a = Context.ObtainStyledAttributes (attrs, Resource.Styleable.ColorPickerPreference);

			showDialogTitle = a.GetBoolean(Resource.Styleable.ColorPickerPreference_showDialogTitle, false);
			showPreviewSelectedColorInList = a.GetBoolean(Resource.Styleable.ColorPickerPreference_showSelectedColorInList, true);

			a.Recycle();	
			a = Context.ObtainStyledAttributes(attrs, Resource.Styleable.ColorPickerView);

			alphaChannelVisible = a.GetBoolean(Resource.Styleable.ColorPickerView_alphaChannelVisible, false);
			alphaChannelText = a.GetString(Resource.Styleable.ColorPickerView_alphaChannelText);		
			colorPickerSliderColor = a.GetColor(Resource.Styleable.ColorPickerView_colorPickerSliderColor, -1);
			colorPickerBorderColor = a.GetColor(Resource.Styleable.ColorPickerView_colorPickerBorderColor, -1);

			a.Recycle();

			if(showPreviewSelectedColorInList) {

				WidgetLayoutResource = Resource.Layout.preference_preview_layout;
			}

			if(!showDialogTitle) {
				DialogTitle = null;
			}

			DialogLayoutResource = Resource.Layout.dialog_color_picker;
			   

			SetPositiveButtonText(Resource.String.dialog_ok);
			SetNegativeButtonText(Resource.String.dialog_cancel);		

			Persistent = true;
			 
		}

		protected override IParcelable OnSaveInstanceState ()
		{
			IParcelable superState = base.OnSaveInstanceState();

			// Create instance of custom BaseSavedState
			SavedState myState = new SavedState(superState);
			// Set the state's value with the class member that holds current setting value


			if(Dialog != null && mColorPickerView != null) {
				myState.currentColor = mColorPickerView.getColor();
			}
			else {
				myState.currentColor = 0;
			}

			return myState;
 		}
		 
		protected override void OnRestoreInstanceState (IParcelable state)
		{
			base.OnRestoreInstanceState (state);

			if (state == null || !(state.GetType().Equals(new SavedState(state).GetType()))) {
				// Didn't save state for us in onSaveInstanceState
				base.OnRestoreInstanceState (state);
				return;
			}

			SavedState myState = (SavedState) state;
			base.OnRestoreInstanceState(myState.SuperState);
//			showDialog(myState.dialogBundle);
			   

			// Set this Preference's widget to reflect the restored state
			if(Dialog != null && mColorPickerView != null) {
				mColorPickerView.setColor(myState.currentColor, true);
			}

		}

		protected override void OnBindView (View view)
		{
			base.OnBindView (view);

			ColorPanelView preview = (ColorPanelView) view.FindViewById(Resource.Id.preference_preview_color_panel);

			if(preview != null) {
				preview.setColor(mColor);
			}
		}


		protected override void OnBindDialogView (View view)
		{
			base.OnBindDialogView (view);

			System.Boolean isLandscapeLayout = false;

			mColorPickerView = (ColorPickerView)view.FindViewById(Resource.Id.color_picker_view);

			LinearLayout landscapeLayout = (LinearLayout) view.FindViewById(Resource.Id.dialog_color_picker_extra_layout_landscape);

			if(landscapeLayout != null) {
				isLandscapeLayout = true;
			}


			mColorPickerView = (ColorPickerView) view.FindViewById(Resource.Id.color_picker_view);
			mOldColorView = (ColorPanelView) view.FindViewById(Resource.Id.color_panel_old);
			mNewColorView = (ColorPanelView) view.FindViewById(Resource.Id.color_panel_new);

			if(!isLandscapeLayout) {
				((LinearLayout) mOldColorView.Parent).SetPadding(
					(int)System.Math.Round(mColorPickerView.getDrawingOffset()), 
					0, 
					(int)System.Math.Round(mColorPickerView.getDrawingOffset()), 
					0);

			}
			else {
				landscapeLayout.SetPadding(0, 0, (int)System.Math.Round(mColorPickerView.getDrawingOffset()), 0);
			}

			mColorPickerView.setAlphaSliderVisible(alphaChannelVisible);
			mColorPickerView.setAlphaSliderText(alphaChannelText);		
			mColorPickerView.setSliderTrackerColor(colorPickerSliderColor);

			if(colorPickerSliderColor != -1) {
				mColorPickerView.setSliderTrackerColor(colorPickerSliderColor);
			}

			if(colorPickerBorderColor != -1) {
				mColorPickerView.setBorderColor(colorPickerBorderColor);
			}


			mColorPickerView.setOnColorChangedListener(this);

			//Log.d("mColorPicker", "setting initial color!");
			mOldColorView.setColor(mColor);
			mColorPickerView.setColor(mColor, true);
		}
		 

		protected override void OnDialogClosed (bool positiveResult)
		{
			//base.OnDialogClosed (positiveResult);

			if(positiveResult) {
				mColor = mColorPickerView.getColor();
				PersistInt(mColor);

				NotifyChanged();

			}
		}
		 
		protected override void OnSetInitialValue (bool restorePersistedValue, Java.Lang.Object defaultValue)
		{
 
			if(restorePersistedValue) {
				//TODO: Cross check the conversion
				mColor = GetPersistedInt (Int32.Parse("FF000000", System.Globalization.NumberStyles.HexNumber));// getPersistedInt(0xFF000000);
			}
			else {
				mColor = (int)defaultValue;
				PersistInt(mColor);
			}
		}
	 
		protected override Java.Lang.Object OnGetDefaultValue (TypedArray a, int index)
		{
			//TODO: cross check with the native app
			return base.OnGetDefaultValue (a, index);
		}
	     
		public void onColorChanged(int newColor) {
			mNewColorView.setColor(newColor);
		}

	    private class SavedState : BaseSavedState {
			public int currentColor;

			public SavedState(IParcelable superState) : base(superState) {
				 
			}

			public SavedState(Parcel source) : base(source) {
				currentColor = source.ReadInt(); 
			}

			public override void WriteToParcel (Parcel dest, ParcelableWriteFlags flags)
			{
				base.WriteToParcel (dest, flags);
				dest.WriteInt (currentColor);
			}

			//TODO: To convert into C# code

			// Standard creator object using an instance of this class
//			public static IParcelableCreator<SavedState> CREATOR =
//				new Parcelable.Creator<SavedState>() {
//
//				public SavedState createFromParcel(Parcel in) {
//					return new SavedState(in);
//				}
//
//				public SavedState[] newArray(int size) {
//					return new SavedState[size];
//				}
//			};

		} 
	}
}

