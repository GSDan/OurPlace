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
using Android.App;
using Android.Media;
using Java.IO;
using Java.Nio;
using OurPlace.Android.Activities;
using OurPlace.Android.Fragments;

namespace OurPlace.Android.Listeners
{
    public class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        private Camera2Fragment Fragment;
        private File File;

        public ImageAvailableListener(Camera2Fragment frag, File file)
        {
            this.Fragment = frag;
            this.File = file;
        }

        public void OnImageAvailable(ImageReader reader)
        {
            Image image = null;
            try
            {
                image = reader.AcquireLatestImage();
                ByteBuffer buffer = image.GetPlanes()[0].Buffer;
                byte[] bytes = new byte[buffer.Capacity()];
                buffer.Get(bytes);
                Save(bytes);

                if (Fragment != null && File != null)
                {
                    Activity activity = Fragment.Activity;
                    if (activity != null)
                    {
                        Fragment.OnPause();
                        ((CameraActivity)activity).ReturnWithFile(File.ToString());
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                System.Console.WriteLine(ex.Message);
            }
            catch (IOException ex)
            {
                System.Console.WriteLine(ex.Message);
            }
            finally
            {
                if (image != null)
                    image.Close();
            }
        }

        private void Save(byte[] bytes)
        {
            OutputStream output = null;
            try
            {
                if (File != null)
                {
                    output = new FileOutputStream(File);
                    output.Write(bytes);
                }
            }
            finally
            {
                if (output != null)
                    output.Close();
            }

            global::Android.Graphics.Bitmap bitmap = global::Android.Graphics.BitmapFactory.DecodeFile(File.AbsolutePath);
            ExifInterface exif = new ExifInterface(File.AbsolutePath);
            string orientation = exif.GetAttribute(ExifInterface.TagOrientation);

            switch (orientation)
            {
                case "6":
                    bitmap = Camera1Fragment.Rotate(bitmap, 90);
                    break;
                case "8":
                    bitmap = Camera1Fragment.Rotate(bitmap, 270);
                    break;
                case "3":
                    bitmap = Camera1Fragment.Rotate(bitmap, 180);
                    break;
            }

            var suppress = AndroidUtils.WriteBitmapToFile(File.AbsolutePath, bitmap);
        }
    }
}