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

// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace OurPlace.iOS
{
    [Register ("DrawingViewController")]
    partial class DrawingViewController
    {
        [Outlet]
        UIKit.UIImageView BGImage { get; set; }


        [Outlet]
        UIKit.UIImageView BrushPreview { get; set; }


        [Outlet]
        UIKit.UISlider BrushSlider { get; set; }


        [Outlet]
        UIKit.UIImageView Canvas { get; set; }


        [Outlet]
        UIKit.UIButton ChangeColourButton { get; set; }


        [Outlet]
        UIKit.UIControl Colour1 { get; set; }


        [Outlet]
        UIKit.UIControl Colour2 { get; set; }


        [Outlet]
        UIKit.UIImageView TempCanvas { get; set; }


        [Outlet]
        OurPlace.iOS.TouchCanvas TouchToCanvas { get; set; }


        [Action ("SliderValueChanged:")]
        partial void SliderValueChanged (Foundation.NSObject sender);

        void ReleaseDesignerOutlets ()
        {
            if (BGImage != null) {
                BGImage.Dispose ();
                BGImage = null;
            }

            if (BrushPreview != null) {
                BrushPreview.Dispose ();
                BrushPreview = null;
            }

            if (BrushSlider != null) {
                BrushSlider.Dispose ();
                BrushSlider = null;
            }

            if (Canvas != null) {
                Canvas.Dispose ();
                Canvas = null;
            }

            if (ChangeColourButton != null) {
                ChangeColourButton.Dispose ();
                ChangeColourButton = null;
            }

            if (Colour1 != null) {
                Colour1.Dispose ();
                Colour1 = null;
            }

            if (Colour2 != null) {
                Colour2.Dispose ();
                Colour2 = null;
            }

            if (TempCanvas != null) {
                TempCanvas.Dispose ();
                TempCanvas = null;
            }

            if (TouchToCanvas != null) {
                TouchToCanvas.Dispose ();
                TouchToCanvas = null;
            }
        }
    }
}