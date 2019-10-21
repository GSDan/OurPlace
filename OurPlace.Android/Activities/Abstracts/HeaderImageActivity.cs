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
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.Design.Widget;
using Android.Support.V7.Graphics;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Views;
using OurPlace.Common;
using System;

namespace OurPlace.Android.Activities.Abstracts
{
    public abstract class HeaderImageActivity : TTSActivity
    {
        protected void LoadHeaderImage(string imageUrl)
        {
            using (ImageViewAsync headerImage = FindViewById<ImageViewAsync>(Resource.Id.backdrop))
            {
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    ImageService.Instance.LoadCompiledResource("logoRect").Into(headerImage);
                    return;
                }

                using(var collapsingToolbar = FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar))
                {
                    ImageService.Instance.LoadUrl(ServerUtils.GetUploadUrl(imageUrl))
                    .Success(() =>
                    {
                        try
                        {
                            if ((BitmapDrawable)headerImage.Drawable != null)
                            {
                                Bitmap headerBitmap = ((BitmapDrawable)headerImage.Drawable).Bitmap;
                                if (headerBitmap != null && !headerBitmap.IsRecycled)
                                {
                                    Palette palette = Palette.From(headerBitmap).Generate();

                                    if (palette.VibrantSwatch != null)
                                    {
                                        Color taskColor = new Color(palette.VibrantSwatch.Rgb);
                                        Color statusColor = new Color(palette.VibrantSwatch.Rgb);
                                        if (palette.DarkVibrantSwatch != null)
                                        {
                                            statusColor = new Color(palette.DarkVibrantSwatch.Rgb);
                                        }

                                        RunOnUiThread(() =>
                                        {
                                            Window.SetStatusBarColor(statusColor);
                                            collapsingToolbar.SetContentScrimColor(taskColor);
                                            collapsingToolbar.SetBackgroundColor(taskColor);
                                        });
                                    }
                                }
                            }
                            else
                            {
                                Toast.MakeText(this, "Image load error", ToastLength.Short).Show();
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }

                    })
                    .Into(headerImage);
                }
            }
        }
    }
}