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
using System.IO;
using AVFoundation;
using CoreMedia;
using CoreVideo;
using FFImageLoading;
using Foundation;
using OurPlace.iOS.Controllers.Tasks;
using OurPlace.Common;
using OurPlace.Common.Models;
using UIKit;

namespace OurPlace.iOS
{
    public partial class CameraViewController : TaskViewController, IAVCapturePhotoCaptureDelegate, IAVCaptureFileOutputRecordingDelegate
    {
        // uses code from 
        // https://blog.xamarin.com/how-to-display-camera-ios-avfoundation/
        // https://forums.xamarin.com/discussion/83230/avcapturephotosettings-fromformat-broken-set-compression
        // https://forums.xamarin.com/discussion/34286/saving-photo-with-meta-data-to-app-directory
        // https://github.com/xamarin/ios-samples/blob/master/ios10/AVCam/AVCam/CameraViewController.cs

        // Video tasks only
        private AVCaptureMovieFileOutput movieOutput;
        private nint backgroundRecordingId;

        // Photo tasks only
        private AVCapturePhotoOutput stillImageOutput;

        private string fileName;
        private string innerPath;
        private string filePath;
        private string folderName;
        private bool isMovie;
        private AVCaptureSession captureSession;
        private AVCaptureDeviceInput captureDeviceInput;
        private AVCaptureVideoPreviewLayer videoPreviewLayer;
        private List<AVCaptureDevice> devices;
        private AVCaptureDeviceDiscoverySession discSession = AVCaptureDeviceDiscoverySession.Create(
                new AVCaptureDeviceType[] { AVCaptureDeviceType.BuiltInWideAngleCamera, AVCaptureDeviceType.BuiltInDualCamera },
                AVMediaType.Video, AVCaptureDevicePosition.Unspecified);
        

		public CameraViewController (IntPtr handle) : base (handle)
		{
            
		}

        #pragma warning disable CS4014
        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Sanity check for permissions - if we're here, it should be safe
            bool hasPerm = await AppUtils.AuthorizeCamera();
            if(!hasPerm)
            {
                AppUtils.ShowSimpleDialog(this, "Requires Camera", "Please grant OurPlace camera access in the system settings to complete this task!", "Ok");
                NavigationController.PopViewController(true);
                return;
            }

            if(thisTask == null)
            {
                AppUtils.ShowSimpleDialog(this, "ERROR", "Error loading task data", "Ok");
                NavigationController.PopViewController(true);
                return;
            }

            folderName = Common.LocalData.Storage.GetCacheFolder(thisActivity.Id.ToString());

            devices = new List<AVCaptureDevice>(discSession.Devices);
            SwapCameraButton.Enabled &= (devices != null && devices.Count != 0);

            if(thisTask.TaskType.IdName == "MATCH_PHOTO")
            {
				// Load target photo
                OverlayImageView.Alpha = 0.6f;

				string localRes = Common.LocalData.Storage.GetCacheFilePath(
                    thisTask.JsonData,
                    thisActivity.Id,
                    ServerUtils.GetFileExtension(thisTask.TaskType.IdName)); 
				
				ImageService.Instance.LoadFile(localRes).Into(OverlayImageView);
            }
            else if(thisTask.TaskType.IdName == "TAKE_VIDEO")
            {
                isMovie = true;
                ImageService.Instance.LoadCompiledResource("RecordButton").IntoAsync(ShutterButton);

                hasPerm = await AppUtils.AuthorizeMic();
                if (!hasPerm)
                {
                    AppUtils.ShowSimpleDialog(this, "Requires Microphone", "Please grant OurPlace microphone access in the system settings to complete this task!", "Ok");
                    NavigationController.PopViewController(true);
                    return;
                }
            }
            else
            {
                OverlayImageView.Alpha = 0;
            }

            SetupLiveCameraStream();
        }
        #pragma warning restore CS4014

        private void SetupLiveCameraStream()
        {
            captureSession = new AVCaptureSession();

            var viewLayer = CameraFeedView.Layer;
            videoPreviewLayer = new AVCaptureVideoPreviewLayer(captureSession)
            {
                Frame = CameraFeedView.Frame,
                VideoGravity = AVLayerVideoGravity.ResizeAspectFill
            };
            CameraFeedView.Layer.AddSublayer(videoPreviewLayer);

            AVCaptureDevice captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaType.Video);
            ConfigureCameraForDevice(captureDevice);
            captureDeviceInput = AVCaptureDeviceInput.FromDevice(captureDevice);
            captureSession.AddInput(captureDeviceInput);

            if (isMovie)
            {
                // Add audio
                var audioDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Audio);
                var audioDeviceInput = AVCaptureDeviceInput.FromDevice(audioDevice, out NSError audioErr);
                if(audioErr != null)
                {
                    Console.WriteLine("Couldn't create audio device input: " + audioErr.LocalizedDescription);
                }
                if(captureSession.CanAddInput(audioDeviceInput))
                {
                    captureSession.AddInput(audioDeviceInput);
                }
                else
                {
                    Console.WriteLine("Couldn't add audio input to session");
                }

                movieOutput = new AVCaptureMovieFileOutput();
                captureSession.AddOutput(movieOutput);
                captureSession.SessionPreset = AVCaptureSession.Preset1280x720;
                var connection = movieOutput.ConnectionFromMediaType(AVMediaType.Video);
                if(connection != null && connection.SupportsVideoStabilization)
                {
                    connection.PreferredVideoStabilizationMode = AVCaptureVideoStabilizationMode.Auto;
                }
                captureSession.CommitConfiguration();
            }
            else
            {
                stillImageOutput = new AVCapturePhotoOutput();
                stillImageOutput.IsHighResolutionCaptureEnabled = true;
                stillImageOutput.IsLivePhotoCaptureEnabled = false;
                captureSession.AddOutput(stillImageOutput);
                captureSession.CommitConfiguration();
            }

            ShutterButton.Hidden = false;

            captureSession.StartRunning();
        }

        private void SetPaths(string extension)
        {
            fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + extension;
            innerPath = Path.Combine(thisActivity.Id.ToString(), fileName);
            filePath = Path.Combine(folderName, fileName);
        }

        private void ConfigureCameraForDevice(AVCaptureDevice device)
        {
            var error = new NSError();
            if (device.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
            {
                device.LockForConfiguration(out error);
                device.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
                device.UnlockForConfiguration();
            }
            else if (device.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure))
            {
                device.LockForConfiguration(out error);
                device.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
                device.UnlockForConfiguration();
            }
            else if (device.IsWhiteBalanceModeSupported(AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance))
            {
                device.LockForConfiguration(out error);
                device.WhiteBalanceMode = AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance;
                device.UnlockForConfiguration();
            }
        }

        [Export("captureOutput:didFinishProcessingPhotoSampleBuffer:previewPhotoSampleBuffer:resolvedSettings:bracketSettings:error:")]
        public void DidFinishProcessingPhoto(AVCapturePhotoOutput captureOutput,
                                             CMSampleBuffer photoSampleBuffer,
                                             CMSampleBuffer previewPhotoSampleBuffer,
                                             AVCaptureResolvedPhotoSettings resolvedSettings,
                                             AVCaptureBracketedStillImageSettings bracketSettings,
                                             NSError error)
        {
            if (photoSampleBuffer == null || error != null)
            {
                Console.WriteLine("Error taking photo: " + error);
                return;
            }
            
            NSData imageData = AVCapturePhotoOutput.GetJpegPhotoDataRepresentation(photoSampleBuffer, previewPhotoSampleBuffer);
            SetPaths(".jpg");

            UIImage formattedImg = AppUtils.ScaleAndRotateImage(UIImage.LoadFromData(imageData));
            imageData = formattedImg.AsJPEG(0.8f);

            if(imageData.Save(filePath, false, out NSError saveErr))
            {
                Console.WriteLine("Saved photo to: " + filePath);
                ReturnWithData(innerPath);
            }
            else
            {
                Console.WriteLine("ERROR saving to " + fileName + " because " + saveErr.LocalizedDescription);
            }
        }

        partial void TakePhoto(UIButton sender)
        {
            AVCaptureVideoOrientation layerOrientation = videoPreviewLayer.Connection.VideoOrientation;

            if(isMovie)
            {
                ShutterButton.Enabled = false; // disable until recording starts/stops

                if(!movieOutput.Recording)
                {
                    // set up recording
                    if (UIDevice.CurrentDevice.IsMultitaskingSupported)
                    {
                        backgroundRecordingId = UIApplication.SharedApplication.BeginBackgroundTask(null);
                    }

                    AVCaptureConnection connection = movieOutput?.ConnectionFromMediaType(AVMediaType.Video);
                    if (connection != null) connection.VideoOrientation = layerOrientation;

                    SetPaths(".mov");

                    movieOutput.StartRecordingToOutputFile(NSUrl.FromFilename(filePath), this);
                }
                else
                {
                    // finish recording
                    movieOutput.StopRecording();
                }
            }
            else
            {
                AVCapturePhotoSettings photoSettings = AVCapturePhotoSettings.Create();

                // The first format in the array is the preferred format
                if (photoSettings.AvailablePreviewPhotoPixelFormatTypes.Length > 0)
                {
                    photoSettings.PreviewPhotoFormat = new NSDictionary<NSString, NSObject>(CVPixelBuffer.PixelFormatTypeKey, photoSettings.AvailablePreviewPhotoPixelFormatTypes[0]);
                }

                stillImageOutput.CapturePhoto(photoSettings, this);
            }
        }

        partial void SwapCamera(UIButton sender)
        {
            int currentIndex = devices.FindIndex(d => { return captureDeviceInput.Device.UniqueID == d.UniqueID; });
            currentIndex++;
            if (currentIndex >= devices.Count) currentIndex = 0;

            AVCaptureDevice device = devices[currentIndex];

            captureSession.RemoveInput(captureDeviceInput);

            ConfigureCameraForDevice(device);
            captureDeviceInput = AVCaptureDeviceInput.FromDevice(device);
            captureSession.AddInput(captureDeviceInput);
        }

        [Export("captureOutput:didStartRecordingToOutputFileAtURL:fromConnections:")]
        public void DidStartRecording(AVCaptureFileOutput captureOutput, NSUrl outputFileUrl, NSObject[] connections)
        {
            ImageService.Instance.LoadCompiledResource("StopButton").Into(ShutterButton);
            ShutterButton.Enabled = true;
            Console.WriteLine("Started recording to " + outputFileUrl.AbsoluteString);
        }

        public void FinishedRecording(AVCaptureFileOutput captureOutput, NSUrl outputFileUrl, NSObject[] connections, NSError error)
        {
            bool success = true;

            if(error != null)
            {
                Console.WriteLine("ERROR writing movie file: " + error.LocalizedDescription);
                success = ((NSNumber)error.UserInfo[AVErrorKeys.RecordingSuccessfullyFinished]).BoolValue;
            }

            if(success)
            {
                Console.WriteLine("Saved movie to: " + outputFileUrl.Path);
                ReturnWithData(innerPath);
            }
            else
            {
                NavigationController.PopViewController(true);
            }
        }
    }
}
