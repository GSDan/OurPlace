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
using Foundation;
using UIKit;

namespace OurPlace.iOS.ViewSources
{
    //https://stackoverflow.com/questions/15577389/a-simple-uipickerview-in-monotouch-xamarin
    public class MapConfigPickerViewModel : UIPickerViewModel
    {
        private string[] data;

        public MapConfigPickerViewModel(string[] _data)
        {
            data = _data;
        }

        public override nint GetComponentCount(UIPickerView pickerView)
        {
            return 1;
        }

        public override nint GetRowsInComponent(UIPickerView pickerView, nint component)
        {
            return data.Length;
        }

        public override string GetTitle(UIPickerView pickerView, nint row, nint component)
        {
            if (data == null || row < 0 || row >= data.Length) return "Out of bounds";
            return data[row];
        }
    }
}
