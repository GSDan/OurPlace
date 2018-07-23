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
using Android.Content;
using Android.Hardware.Camera2;

namespace OurPlace.Android.Listeners
{
    public class CameraCaptureListener : CameraCaptureSession.CaptureCallback
    {
        public Context context;
        private ProgressDialog loadingDialog;

        public override void OnCaptureStarted(CameraCaptureSession session, CaptureRequest request, long timestamp, long frameNumber)
        {
            loadingDialog = new ProgressDialog(context);
            loadingDialog.SetMessage(context.Resources.GetString(Resource.String.PleaseWait));
            loadingDialog.Indeterminate = true;
            loadingDialog.Show();

            base.OnCaptureStarted(session, request, timestamp, frameNumber);
        }
        public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
        {
            if (loadingDialog != null)
            {
                loadingDialog.Dismiss();
            }

            base.OnCaptureCompleted(session, request, result);
        }
    }
}