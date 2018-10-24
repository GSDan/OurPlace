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
using Android.Hardware;
using Android.Media;
using Android.OS;
using Android.Views;
using Android.Widget;
using OurPlace.Android.Activities;
using System;
using System.IO;

namespace OurPlace.Android.Fragments
{

#pragma warning disable CS0618 // Type or member is obsolete (Camera 1)
    public class Camera1VideoFragment : Fragment
    {
        private Camera camera;
        private CameraPreview preview;
        private MediaRecorder mediaRecorder;
        private Button captureBtn;
        private FrameLayout previewView;
        private bool recording = false;
        private string outputPath;

        public static Camera1VideoFragment NewInstance()
        {
            var fragment = new Camera1VideoFragment();
            fragment.RetainInstance = true;
            return fragment;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Activity.Window.AddFlags(WindowManagerFlags.KeepScreenOn);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.Camera1Fragment, container, false);

            bool opened = SafeCameraOpenInView(view);

            if (!opened)
            {
                Console.WriteLine("Camera failed to open");
                return view;
            }

            captureBtn = view.FindViewById<Button>(Resource.Id.button_capture);
            captureBtn.Click += CaptureBtn_Click;

            return view;
        }

        private bool SafeCameraOpenInView(View view)
        {
            ReleaseCameraAndPreview();
            camera = Camera1Fragment.GetCameraInstance(1280 * 720, true);

            if (camera == null)
            {
                return false;
            }

            preview = new CameraPreview(Activity.BaseContext, camera, view, true);
            previewView = view.FindViewById<FrameLayout>(Resource.Id.camera_preview);
            previewView.AddView(preview, 0);
            preview.StartCameraPreview();

            return true;
        }

        public override void OnResume()
        {
            base.OnResume();
            if (camera != null)
            {
                return;
            }

            camera = Camera1Fragment.GetCameraInstance(1280 * 720);
            preview = new CameraPreview(Activity, camera, View, true);
            previewView.AddView(preview, 0);
            preview.StartCameraPreview();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            ReleaseCameraAndPreview();
        }

        private void ReleaseCameraAndPreview()
        {
            ReleaseMediaRecorder();
            if (camera != null)
            {
                camera.StopPreview();
                camera.Release();
                camera = null;
            }

            if (preview == null)
            {
                return;
            }

            preview.DestroyDrawingCache();
            preview.Camera = null;
        }

        private void ReleaseMediaRecorder()
        {
            if (mediaRecorder == null)
            {
                return;
            }

            mediaRecorder.Reset();
            mediaRecorder.Release();
            mediaRecorder = null;
            camera.Lock();
        }

        private bool PrepareMediaRecorder()
        {
            outputPath = new Java.IO.File(Activity.GetExternalFilesDir(null),
                         DateTime.UtcNow.ToString("MM-dd-yyyy-HH-mm-ss-fff") + ".mp4").AbsolutePath;

            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            Camera.Parameters parameters = camera.GetParameters();
            camera.Unlock();
            mediaRecorder = new MediaRecorder();
            mediaRecorder.SetOutputFile(outputPath);
            mediaRecorder.SetCamera(camera);
            mediaRecorder.SetAudioSource(AudioSource.Camcorder);
            mediaRecorder.SetVideoSource(VideoSource.Camera);
            mediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);
            mediaRecorder.SetVideoFrameRate(30);
            mediaRecorder.SetVideoEncodingBitRate(5000000);
            mediaRecorder.SetVideoEncoder(VideoEncoder.H264);
            mediaRecorder.SetAudioEncoder(AudioEncoder.Aac);
            mediaRecorder.SetAudioSamplingRate(44100);
            mediaRecorder.SetAudioEncodingBitRate(96000);
            mediaRecorder.SetVideoSize(parameters.PictureSize.Width, parameters.PictureSize.Height);
            mediaRecorder.SetMaxDuration(600000); // ten mins

            try
            {
                mediaRecorder.Prepare();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                ReleaseMediaRecorder();
                return false;
            }

            return true;
        }

        private void CaptureBtn_Click(object sender, EventArgs e)
        {
            if(recording)
            {
                mediaRecorder.Stop();
                ReleaseMediaRecorder();
                recording = false;
                ((CameraActivity)Activity).ReturnWithFile(outputPath);
            }
            else
            {
                if(!PrepareMediaRecorder())
                {
                    Toast.MakeText(Activity, Resource.String.errorCamera, ToastLength.Long).Show();
                    Activity.Finish();
                }
                else
                {
                    try
                    {
                        mediaRecorder.Start();
                        recording = true;
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}