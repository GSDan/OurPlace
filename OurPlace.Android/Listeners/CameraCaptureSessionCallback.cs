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
using Android.Hardware.Camera2;
using OurPlace.Android.Fragments;

namespace OurPlace.Android.Listeners
{
    public class CameraCaptureSessionCallback : CameraCaptureSession.StateCallback
    {
        public Camera2Fragment Owner { get; set; }

        public CameraCaptureSessionCallback(Camera2Fragment owner)
        {
            Owner = owner;
        }

        public override void OnConfigureFailed(CameraCaptureSession session)
        {
            Owner.ShowToast("Failed");
        }

        public override void OnConfigured(CameraCaptureSession session)
        {
            // The camera is already closed
            if (null == Owner.MCameraDevice)
            {
                return;
            }

            // When the session is ready, we start displaying the preview.
            Owner.MCaptureSession = session;
            try
            {
                // Auto focus should be continuous for camera preview.
                Owner.MPreviewRequestBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.ContinuousPicture);
                // Flash is automatically enabled when necessary.
                Owner.SetAutoFlash(Owner.MPreviewRequestBuilder);

                // Finally, we start displaying the camera preview.
                Owner.MPreviewRequest = Owner.MPreviewRequestBuilder.Build();
                Owner.MCaptureSession.SetRepeatingRequest(Owner.MPreviewRequest,
                        Owner.MCaptureCallback, Owner.MBackgroundHandler);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }
    }
}