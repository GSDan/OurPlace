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
using System.Drawing;

#if __UNIFIED__
using CoreAnimation;
using Foundation;
using UIKit;
#else
using MonoTouch.CoreAnimation;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace PopColorPicker.iOS
{
    public static class LayerHelper
    {
        public static void SetupShadow(CALayer layer)
        {
            layer.ShadowColor = UIColor.Black.CGColor;
            layer.ShadowOpacity = 0.8f;
            layer.ShadowOffset = new SizeF(0f, 2f);

            var rect = layer.Frame;
            rect.X = 0f;
            rect.Y = 0f;

            layer.ShadowPath = UIBezierPath.FromRoundedRect(rect, layer.CornerRadius).CGPath;
        }

        public static UIColor InverseColor(UIColor color)
        {
            var componentColor = color.CGColor.Components;
            var newColor = UIColor.FromRGBA(1f - componentColor[0], 1f - componentColor[1], 1f - componentColor[2], componentColor[3]);

            return newColor;
        }
    }
}

