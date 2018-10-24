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
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Concurrent;
using OurPlace.Android.Activities;
using System;
using System.Collections.Generic;

namespace OurPlace.Android.Fragments
{
    public class Camera2VideoFragment : Fragment, global::Android.Views.View.IOnClickListener, MediaRecorder.IOnInfoListener
    {
        private const string FragTag = "Camera2VideoFragment";
        private readonly SparseIntArray orientations = new SparseIntArray();
        private string videoPath;

        // Button to record video
        private Button buttonVideo;

        // AutoFitTextureView for camera preview
        public AutoFitTextureView TextureView;

        public CameraDevice CameraDevice;
        public CameraCaptureSession PreviewSession;
        public MediaRecorder MediaRecorder;

        private bool isRecordingVideo;
        public Semaphore CameraOpenCloseLock = new Semaphore(1);

        // Called when the CameraDevice changes state
        private readonly MyCameraStateCallback stateListener;
        // Handles several lifecycle events of a TextureView
        private readonly MySurfaceTextureListener surfaceTextureListener;

        private CaptureRequest.Builder previewBuilder;

        private Size videoSize;
        private Size previewSize;

        private HandlerThread backgroundThread;
        private Handler backgroundHandler;


        public Camera2VideoFragment()
        {
            orientations.Append((int)SurfaceOrientation.Rotation0, 90);
            orientations.Append((int)SurfaceOrientation.Rotation90, 0);
            orientations.Append((int)SurfaceOrientation.Rotation180, 270);
            orientations.Append((int)SurfaceOrientation.Rotation270, 180);
            surfaceTextureListener = new MySurfaceTextureListener(this);
            stateListener = new MyCameraStateCallback(this);
        }
        public static Camera2VideoFragment NewInstance()
        {
            var fragment = new Camera2VideoFragment { RetainInstance = true };
            return fragment;
        }

        public static Size ChooseVideoSize(Size[] choices)
        {
            Size closest = choices[choices.Length - 1];
            const int targetRes = 1280 * 720;
            int currentDiff = System.Math.Abs(closest.Width * closest.Height - targetRes);

            foreach (Size size in choices)
            {
                if (size.Width != size.Height * 4 / 3)
                {
                    continue;
                }

                int thisDiff = System.Math.Abs(size.Width * size.Height - targetRes);
                if (thisDiff >= currentDiff)
                {
                    continue;
                }

                closest = size;
                currentDiff = thisDiff;
            }
            return closest;
        }

        private Size ChooseOptimalSize(Size[] choices, int width, int height, Size aspectRatio)
        {
            var bigEnough = new List<Size>();
            int w = aspectRatio.Width;
            int h = aspectRatio.Height;

            foreach (Size option in choices)
            {
                if (option.Height == option.Width * h / w &&
                    option.Width >= width && option.Height >= height)
                {
                    bigEnough.Add(option);
                }
            }

            if (bigEnough.Count > 0)
            {
                return (Size)Collections.Min(bigEnough, new CompareSizesByArea());
            }
                
            Log.Error(FragTag, "Couldn't find any suitable preview size");
            return choices[0];
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.Camera2VideoFragment, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            TextureView = (AutoFitTextureView)view.FindViewById(Resource.Id.texture);
            buttonVideo = (Button)view.FindViewById(Resource.Id.video);
            buttonVideo.SetOnClickListener(this);
            TextureView.KeepScreenOn = true;
            view.FindViewById(Resource.Id.info).SetOnClickListener(this);
        }

        public override void OnResume()
        {
            base.OnResume();
            StartBackgroundThread();
            if (TextureView.IsAvailable)
            {
                OpenCamera(TextureView.Width, TextureView.Height);
            }
            else
            {
                TextureView.SurfaceTextureListener = surfaceTextureListener;
            }
        }

        public override void OnPause()
        {
            CloseCamera();
            StopBackgroundThread();
            base.OnPause();
        }

        private void StartBackgroundThread()
        {
            backgroundThread = new HandlerThread("CameraBackground");
            backgroundThread.Start();
            backgroundHandler = new Handler(backgroundThread.Looper);
        }

        private void StopBackgroundThread()
        {
            backgroundThread.QuitSafely();
            try
            {
                backgroundThread.Join();
                backgroundThread = null;
                backgroundHandler = null;
            }
            catch (InterruptedException e)
            {
                e.PrintStackTrace();
            }
        }

        public void OnClick(View view)
        {
            switch (view.Id)
            {
                case Resource.Id.video:
                    {
                        if (isRecordingVideo)
                        {
                            StopRecordingVideo();
                        }
                        else
                        {
                            StartRecordingVideo();
                        }
                        break;
                    }

                case Resource.Id.info:
                    {
                        if (null != Activity)
                        {
                            new AlertDialog.Builder(Activity)
                                .SetMessage(((CameraActivity)Activity).learningTask.Description)
                                .SetPositiveButton(global::Android.Resource.String.Ok, (IDialogInterfaceOnClickListener)null)
                                .Show();
                        }
                        break;
                    }
            }
        }

        //Tries to open a CameraDevice
        public void OpenCamera(int width, int height)
        {
            if (null == Activity || Activity.IsFinishing)
            {
                return;
            }

            CameraManager manager = (CameraManager)Activity.GetSystemService(Context.CameraService);
            try
            {
                if (!CameraOpenCloseLock.TryAcquire(2500, TimeUnit.Milliseconds))
                {
                    throw new RuntimeException("Time out waiting to lock camera opening.");
                }

                string cameraId = manager.GetCameraIdList()[0];
                CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);
                StreamConfigurationMap map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                videoSize = ChooseVideoSize(map.GetOutputSizes(Class.FromType(typeof(MediaRecorder))));
                previewSize = ChooseOptimalSize(map.GetOutputSizes(Class.FromType(typeof(MediaRecorder))), width, height, videoSize);
                int orientation = (int)Resources.Configuration.Orientation;
                if (orientation == (int)global::Android.Content.Res.Orientation.Landscape)
                {
                    TextureView.SetAspectRatio(previewSize.Width, previewSize.Height);
                }
                else
                {
                    TextureView.SetAspectRatio(previewSize.Height, previewSize.Width);
                }
                ConfigureTransform(width, height);
                MediaRecorder = new MediaRecorder();
                manager.OpenCamera(cameraId, stateListener, null);

            }
            catch (CameraAccessException)
            {
                Toast.MakeText(Activity, "Cannot access the camera.", ToastLength.Short).Show();
                Activity.Finish();
            }
            catch (NullPointerException)
            {
                var dialog = new ErrorDialog();
                dialog.Show(FragmentManager, "dialog");
            }
            catch (InterruptedException)
            {
                throw new RuntimeException("Interrupted while trying to lock camera opening.");
            }
        }

        //Start the camera preview
        public void StartPreview()
        {
            if (null == CameraDevice || !TextureView.IsAvailable || null == previewSize)
            {
                return;
            }

            try
            {
                SetUpMediaRecorder();
                SurfaceTexture texture = TextureView.SurfaceTexture;
                //Assert.IsNotNull(texture);
                texture.SetDefaultBufferSize(previewSize.Width, previewSize.Height);
                previewBuilder = CameraDevice.CreateCaptureRequest(CameraTemplate.Record);
                var surfaces = new List<Surface>();
                var previewSurface = new Surface(texture);
                surfaces.Add(previewSurface);
                previewBuilder.AddTarget(previewSurface);

                var recorderSurface = MediaRecorder.Surface;
                surfaces.Add(recorderSurface);
                previewBuilder.AddTarget(recorderSurface);

                CameraDevice.CreateCaptureSession(surfaces, new PreviewCaptureStateCallback(this), backgroundHandler);

            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
            catch (IOException e)
            {
                e.PrintStackTrace();
            }
        }

        private void CloseCamera()
        {
            try
            {
                CameraOpenCloseLock.Acquire();
                if (null != CameraDevice)
                {
                    CameraDevice.Close();
                    CameraDevice = null;
                }
                if (null != MediaRecorder)
                {
                    MediaRecorder.Release();
                    MediaRecorder = null;
                }
            }
            catch (InterruptedException e)
            {
                System.Console.WriteLine(e.Message);
                throw new RuntimeException("Interrupted while trying to lock camera closing.");
            }
            finally
            {
                CameraOpenCloseLock.Release();
            }
        }

        //Update the preview
        public void UpdatePreview()
        {
            if (null == CameraDevice)
            {
                return;
            }

            try
            {
                SetUpCaptureRequestBuilder(previewBuilder);
                HandlerThread thread = new HandlerThread("CameraPreview");
                thread.Start();
                PreviewSession.SetRepeatingRequest(previewBuilder.Build(), null, backgroundHandler);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }

        private static void SetUpCaptureRequestBuilder(CaptureRequest.Builder builder)
        {
            builder.Set(CaptureRequest.ControlMode, new Integer((int)ControlMode.Auto));

        }

        //Configures the necessary matrix transformation to apply to the textureView
        public void ConfigureTransform(int viewWidth, int viewHeight)
        {
            if (null == Activity || null == previewSize || null == TextureView)
            {
                return;
            }

            int rotation = (int)Activity.WindowManager.DefaultDisplay.Rotation;
            var matrix = new Matrix();
            var viewRect = new RectF(0, 0, viewWidth, viewHeight);
            var bufferRect = new RectF(0, 0, previewSize.Height, previewSize.Width);
            float centreX = viewRect.CenterX();
            float centreY = viewRect.CenterY();

            if ((int)SurfaceOrientation.Rotation90 == rotation || (int)SurfaceOrientation.Rotation270 == rotation)
            {
                bufferRect.Offset((centreX - bufferRect.CenterX()), (centreY - bufferRect.CenterY()));
                matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
                float scale = System.Math.Max(
                    (float)viewHeight / previewSize.Height,
                    (float)viewHeight / previewSize.Width);
                matrix.PostScale(scale, scale, centreX, centreY);
                matrix.PostRotate(90 * (rotation - 2), centreX, centreY);
            }
            TextureView.SetTransform(matrix);
        }

        private void SetUpMediaRecorder()
        {
            if (null == Activity)
            {
                return;
            }

            videoPath = GetVideoFile(Activity).AbsolutePath;

            MediaRecorder.SetAudioSource(AudioSource.Mic);
            MediaRecorder.SetVideoSource(VideoSource.Surface);
            MediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);
            MediaRecorder.SetOutputFile(videoPath);
            MediaRecorder.SetVideoEncodingBitRate(5000000);
            MediaRecorder.SetVideoFrameRate(30);
            MediaRecorder.SetVideoSize(videoSize.Width, videoSize.Height);
            MediaRecorder.SetVideoEncoder(VideoEncoder.H264);
            MediaRecorder.SetAudioEncoder(AudioEncoder.Aac);
            MediaRecorder.SetAudioSamplingRate(44100);
            MediaRecorder.SetAudioEncodingBitRate(96000);
            MediaRecorder.SetMaxDuration(600000); // Ten min
            MediaRecorder.SetOnInfoListener(this);
            int rotation = (int)Activity.WindowManager.DefaultDisplay.Rotation;
            int orientation = orientations.Get(rotation);
            MediaRecorder.SetOrientationHint(orientation);
            MediaRecorder.Prepare();
        }

        private static File GetVideoFile(Context context)
        {
            return new File(context.GetExternalFilesDir(null), DateTime.UtcNow.ToString("MM-dd-yyyy-HH-mm-ss-fff") + ".mp4");
        }

        private void StartRecordingVideo()
        {
            try
            {
                //UI
                buttonVideo.SetText(Resource.String.StopBtn);
                isRecordingVideo = true;

                //Start recording
                MediaRecorder.Start();
            }
            catch (IllegalStateException e)
            {
                e.PrintStackTrace();
            }
        }

        public void StopRecordingVideo()
        {
            //UI
            isRecordingVideo = false;
            buttonVideo.SetText(Resource.String.StartBtn);

            // Workaround for https://github.com/googlesamples/android-Camera2Video/issues/2
            CloseCamera();

            ((CameraActivity)Activity).ReturnWithFile(videoPath);
        }

        public void OnInfo(MediaRecorder mr, [GeneratedEnum] MediaRecorderInfo what, int extra)
        {
            if (what != MediaRecorderInfo.MaxDurationReached)
            {
                return;
            }

            Toast.MakeText(Activity, Resource.String.recordingLimit, ToastLength.Long).Show();
            StopRecordingVideo();
        }

        public class ErrorDialog : DialogFragment
        {
            public override Dialog OnCreateDialog(Bundle savedInstanceState)
            {
                var alert = new AlertDialog.Builder(Activity);
                alert.SetMessage("This device doesn't support Camera2 API.");
                alert.SetPositiveButton(global::Android.Resource.String.Ok, new MyDialogOnClickListener(this));
                return alert.Show();
            }
        }

        private class MyDialogOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
        {
            private readonly ErrorDialog er;
            public MyDialogOnClickListener(ErrorDialog e)
            {
                er = e;
            }
            public void OnClick(IDialogInterface dialogInterface, int i)
            {
                er.Activity.Finish();
            }
        }

        // Compare two Sizes based on their areas
        private class CompareSizesByArea : Java.Lang.Object, Java.Util.IComparator
        {
            public int Compare(Java.Lang.Object lhs, Java.Lang.Object rhs)
            {
                // We cast here to ensure the multiplications won't overflow
                if (lhs is Size left && rhs is Size right)
                {
                    return Long.Signum((long)left.Width * left.Height -
                        (long)right.Width * right.Height);
                }

                return 0;
            }
        }

        public class MyCameraStateCallback : CameraDevice.StateCallback
        {
            private readonly Camera2VideoFragment fragment;
            public MyCameraStateCallback(Camera2VideoFragment frag)
            {
                fragment = frag;
            }
            public override void OnOpened(CameraDevice camera)
            {
                fragment.CameraDevice = camera;
                fragment.StartPreview();
                fragment.CameraOpenCloseLock.Release();
                if (null != fragment.TextureView)
                {
                    fragment.ConfigureTransform(fragment.TextureView.Width, fragment.TextureView.Height);
                }
            }

            public override void OnDisconnected(CameraDevice camera)
            {
                fragment.CameraOpenCloseLock.Release();
                camera.Close();
                fragment.CameraDevice = null;
            }

            public override void OnError(CameraDevice camera, CameraError error)
            {
                fragment.CameraOpenCloseLock.Release();
                camera.Close();
                fragment.CameraDevice = null;

                fragment.Activity?.Finish();
            }
        }

        public class MySurfaceTextureListener : Java.Lang.Object, TextureView.ISurfaceTextureListener
        {
            private readonly Camera2VideoFragment fragment;
            public MySurfaceTextureListener(Camera2VideoFragment frag)
            {
                fragment = frag;
            }

            public void OnSurfaceTextureAvailable(SurfaceTexture surfaceTexture, int width, int height)
            {
                fragment.OpenCamera(width, height);
            }

            public void OnSurfaceTextureSizeChanged(SurfaceTexture surfaceTexture, int width, int height)
            {
                fragment.ConfigureTransform(width, height);
            }

            public bool OnSurfaceTextureDestroyed(SurfaceTexture surfaceTexture)
            {
                return true;
            }

            public void OnSurfaceTextureUpdated(SurfaceTexture surfaceTexture)
            {
            }

        }

        public class PreviewCaptureStateCallback : CameraCaptureSession.StateCallback
        {
            private readonly Camera2VideoFragment fragment;
            public PreviewCaptureStateCallback(Camera2VideoFragment frag)
            {
                fragment = frag;
            }
            public override void OnConfigured(CameraCaptureSession session)
            {
                fragment.PreviewSession = session;
                fragment.UpdatePreview();

            }

            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                if (fragment.Activity != null)
                {
                    Toast.MakeText(fragment.Activity, "Failed", ToastLength.Short).Show();
                }
            }
        }
    }
}