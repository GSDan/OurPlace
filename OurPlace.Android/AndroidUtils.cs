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
using Android.Content.PM;
using Android.Gms.Common;
using Android.Graphics;
using Android.Media;
using Android.Provider;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Util;
using Android.Views;
using FFImageLoading;
using FFImageLoading.Views;
using Java.Lang;
using Java.Util;
using OurPlace.Android.Activities;
using OurPlace.Android.Activities.Create;
using OurPlace.Common;
using OurPlace.Common.LocalData;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using static OurPlace.Common.LocalData.Storage;

namespace OurPlace.Android
{
    public static class AndroidUtils
    {
        public static void LoadTaskTypeIcon(TaskType type, ImageViewAsync imageView)
        {
            string imageRes = null;

            switch (type.IdName)
            {
                case "INFO":
                    imageRes = "task_info";
                    break;
                case "LISTEN_AUDIO":
                    imageRes = "task_listen";
                    break;
                case "TAKE_PHOTO":
                    imageRes = "task_photo";
                    break;
                case "MATCH_PHOTO":
                    imageRes = "task_photoMatch";
                    break;
                case "DRAW":
                    imageRes = "task_draw";
                    break;
                case "DRAW_PHOTO":
                    imageRes = "task_drawPhoto";
                    break;
                case "TAKE_VIDEO":
                    imageRes = "task_recVideo";
                    break;
                case "REC_AUDIO":
                    imageRes = "task_recAudio";
                    break;
                case "MAP_MARK":
                    imageRes = "task_mapMark";
                    break;
                case "LOC_HUNT":
                    imageRes = "task_locHunt";
                    break;
                case "SCAN_QR":
                    imageRes = "task_scan";
                    break;
                case "MULT_CHOICE":
                    imageRes = "task_multChoice";
                    break;
                case "ENTER_TEXT":
                    imageRes = "task_text";
                    break;
                default:
                    imageRes = "OurPlace_logo";
                    break;
            }

            ImageService.Instance.LoadCompiledResource(imageRes).Into(imageView);
        }

        public static async Task<bool> PrepActivityFiles(Context context, LearningActivity act)
        {
            ProgressDialog loadingDialog = new ProgressDialog(context);
            loadingDialog.SetTitle(Resource.String.PleaseWait);
            loadingDialog.SetMessage(context.Resources.GetString(Resource.String.actLoadStart));
            loadingDialog.Indeterminate = true;
            loadingDialog.SetCancelable(false);
            loadingDialog.Show();

            string baseMessage = context.Resources.GetString(Resource.String.actLoadMessage);

            // Get all tasks in this activity which use uploaded files
            List<TaskFileInfo> fileUrls = GetFileTasks(act);

            using (WebClient webClient = new WebClient())
            {
                // Loop over and pre-prepare listed files
                for (int i = 0; i < fileUrls.Count; i++)
                {
                    loadingDialog.SetMessage(string.Format(baseMessage, i + 1, fileUrls.Count));
                    string thisUrl = ServerUtils.GetUploadUrl(fileUrls[i].fileUrl);
                    string cachePath = GetCacheFilePath(thisUrl, act.Id, fileUrls[i].extension);
                    if (File.Exists(cachePath))
                    {
                        continue;
                    }

                    try
                    {
                        await webClient.DownloadFileTaskAsync(new Uri(thisUrl), cachePath);
                    }
                    catch (System.Exception e)
                    {
                        File.Delete(cachePath);
                        Console.WriteLine(e.Message);
                        return false;
                    }
                }
            }

            loadingDialog.Dismiss();
            return true;
        }

        public static void LoadActivityImageIntoView(ImageViewAsync targetImageView, string imageUrl, int activityId, int quality = 350)
        {
            string localRes = GetCacheFilePath(imageUrl, activityId, "jpg");

            if (!File.Exists(localRes))
            {
                // Image hasn't been locally cached, try to download remote version
                ImageService.Instance.LoadUrl(ServerUtils.GetUploadUrl(imageUrl))
                    .DownSample(quality)
                    .Into(targetImageView);
            }
            else
            {
                // Load the local file
                ImageService.Instance.LoadFile(localRes).DownSample(500)
                    .Into(targetImageView);
            }

            targetImageView.Visibility = ViewStates.Visible;
        }

        public static async Task ReturnToSignIn(Activity activity)
        {
            try
            {
                DatabaseManager dbManager = await GetDatabaseManager();

                if (dbManager != null)
                {
                    dbManager.DeleteLearnerCacheAndProgress();
                    dbManager.CleanDatabase();
                }

                if (activity == null)
                {
                    return;
                }

                Context context = activity.ApplicationContext;
                Intent intent = new Intent(context, typeof(LoginActivity));
                activity.StartActivity(intent);
                activity.Finish();
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static void LocationToEXIF(string filePath, global::Android.Locations.Location loc)
        {
            try
            {
                ExifInterface ef = new ExifInterface(filePath);
                ef.SetAttribute(ExifInterface.TagGpsLatitude, Helpers.DecToDMS(loc.Latitude));
                ef.SetAttribute(ExifInterface.TagGpsLongitude, Helpers.DecToDMS(loc.Longitude));

                if (loc.Latitude > 0)
                {
                    ef.SetAttribute(ExifInterface.TagGpsLatitudeRef, "N");
                }
                else
                {
                    ef.SetAttribute(ExifInterface.TagGpsLatitudeRef, "S");
                }

                if (loc.Longitude > 0)
                {
                    ef.SetAttribute(ExifInterface.TagGpsLongitudeRef, "E");
                }
                else
                {
                    ef.SetAttribute(ExifInterface.TagGpsLongitudeRef, "W");
                }

                ef.SaveAttributes();
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static int ConvertPixelsToDp(float pixelValue, Context context)
        {
            var dp = (int)((pixelValue) / context.Resources.DisplayMetrics.Density);
            return dp;
        }

        public static void CheckGetPermission(string permission, Activity context, int requestId, string title, string message)
        {
            if (ContextCompat.CheckSelfPermission(context, permission) != Permission.Granted)
            {
                // Show an explanation of why it's needed if necessary
                if (ActivityCompat.ShouldShowRequestPermissionRationale(context, permission))
                {
                    global::Android.Support.V7.App.AlertDialog dialog = new global::Android.Support.V7.App.AlertDialog.Builder(context)
                        .SetTitle(title)
                        .SetMessage(message)
                        .SetPositiveButton("Got it", (s, e) =>
                        {
                            ActivityCompat.RequestPermissions(context, new string[] { permission }, requestId);
                        })
                        .Create();
                    dialog.Show();
                }
                else
                {
                    // No explanation needed, just ask
                    ActivityCompat.RequestPermissions(context, new string[] { permission }, requestId);
                }
            }
        }

        public static async void CallWithPermission(string[] perms, string[] explanationTitles, string[] explanations, Intent toCall, int intentId, int permReqId, Activity activity)
        {
            List<string> neededPerms = new List<string>();
            int accountedFor = 0;

            for (int i = 0; i < perms.Length; i++)
            {
                if (ContextCompat.CheckSelfPermission(activity, perms[i]) != Permission.Granted)
                {
                    // Haven't got the permision yet
                    string thisPerm = perms[i];

                    // Show an explanation of why it's needed if necessary
                    if (ActivityCompat.ShouldShowRequestPermissionRationale(activity, perms[i]))
                    {
                        global::Android.Support.V7.App.AlertDialog dialog = new global::Android.Support.V7.App.AlertDialog.Builder(activity)
                            .SetTitle(explanationTitles[i])
                            .SetMessage(explanations[i])
                            .SetPositiveButton("Got it", (s, e) =>
                            {
                                neededPerms.Add(thisPerm);
                                accountedFor++;
                            })
                            .Create();
                        dialog.Show();
                    }
                    else
                    {
                        // No explanation needed, just ask
                        neededPerms.Add(perms[i]);
                        accountedFor++;
                    }
                }
                else
                {
                    accountedFor++;
                }
            }

            while (accountedFor < perms.Length)
            {
                await Task.Delay(20);
            }

            if (neededPerms.Count == 0)
            {
                activity.StartActivityForResult(toCall, intentId);
            }
            else
            {
                ActivityCompat.RequestPermissions(activity, neededPerms.ToArray(), permReqId);
            }
        }

        public static bool IsGooglePlayServicesInstalled(Activity context)
        {
            int queryResult = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(context);
            if (queryResult == ConnectionResult.Success)
            {
                return true;
            }

            if (GoogleApiAvailability.Instance.IsUserResolvableError(queryResult) && context != null)
            {
                // Show error dialog to let user debug google play services
                string errorString = GoogleApiAvailability.Instance.GetErrorString(queryResult);

                new global::Android.Support.V7.App.AlertDialog.Builder(context)
                    .SetTitle("Error")
                    .SetMessage(string.Format("There is a problem with Google Play Services on this device: {0} - {1}", queryResult, errorString))
                    .Show();
            }
            return false;
        }

        public static Intent CreateMultiSourceImagePickerIntent(bool includeCamera, global::Android.Net.Uri outputFileUri, Context context)
        {
            Intent galleryIntent = new Intent();
            galleryIntent.SetType("image/*");
            galleryIntent.SetAction(Intent.ActionPick);
            Intent finalIntent;

            if (includeCamera)
            {
                //http://stackoverflow.com/questions/4455558/allow-user-to-select-camera-or-gallery-for-image/12347567#12347567
                List<Intent> cameraIntents = new List<Intent>();
                Intent captureIntent = new Intent(MediaStore.ActionImageCapture);
                IList<ResolveInfo> listCam = context.PackageManager.QueryIntentActivities(captureIntent, PackageInfoFlags.MatchAll);

                foreach (ResolveInfo res in listCam)
                {
                    string packageName = res.ActivityInfo.PackageName;
                    Intent intent = new Intent(captureIntent);
                    intent.SetComponent(new ComponentName(packageName, res.ActivityInfo.Name));
                    intent.SetPackage(packageName);
                    intent.PutExtra(MediaStore.ExtraOutput, outputFileUri);
                    cameraIntents.Add(intent);
                }

                finalIntent = Intent.CreateChooser(galleryIntent, "Select an Image Source");
                finalIntent.PutExtra(Intent.ExtraInitialIntents, cameraIntents.ToArray());
            }
            else
            {
                finalIntent = galleryIntent;
            }

            return finalIntent;
        }

        public static async Task WriteBitmapToFile(string path, Bitmap bm)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                await bm.CompressAsync(Bitmap.CompressFormat.Jpeg, 90, stream);
                byte[] bitmapData = stream.ToArray();
                WriteDataToFile(path, bitmapData);
                stream.Dispose();
            }

            bm.Recycle();
        }

        public static async Task<global::Android.Net.Uri> OnImagePickerResult([GeneratedEnum] Result resultCode, Intent data, global::Android.Net.Uri outputFileUri, Context context, string finalPath, int largeSide, int shortSide)
        {
            bool isCamera;
            global::Android.Net.Uri selectedImage;

            if (data == null || data.Data == null)
            {
                isCamera = true;
            }
            else
            {
                string action = data.Action;
                if (action == null)
                {
                    isCamera = false;
                }
                else
                {
                    isCamera = action == MediaStore.ActionImageCapture;
                }
            }

            if (isCamera)
            {
                selectedImage = outputFileUri;
            }
            else
            {
                selectedImage = (data == null) ? null : data.Data;
                if (selectedImage != null)
                {
                    System.IO.Stream stream = context.ContentResolver.OpenInputStream(selectedImage);
                    if (File.Exists(outputFileUri.Path))
                    {
                        File.Delete(outputFileUri.Path);
                    }

                    using (var fileStream = File.Create(outputFileUri.Path))
                    {
                        await stream.CopyToAsync(fileStream);
                        selectedImage = outputFileUri;
                    }
                }
            }

            if (selectedImage == null || !File.Exists(selectedImage.Path))
            {
                return null;
            }

            // Scale the image down to a maximum size
            Bitmap fullBitmap = await BitmapFactory.DecodeFileAsync(selectedImage.Path);
            int currentSize = fullBitmap.Width * fullBitmap.Height;
            int maxSize = largeSide * shortSide;

            if (currentSize <= maxSize)
            {
                // Small enough, save without resizing
                await WriteBitmapToFile(finalPath, fullBitmap);
            }
            else
            {
                float scale = (float)maxSize / (float)currentSize;

                // CREATE A MATRIX FOR THE MANIPULATION
                Matrix matrix = new Matrix();
                // RESIZE THE BIT MAP
                matrix.PostScale(scale, scale);

                // "RECREATE" THE NEW BITMAP
                Bitmap resizedBitmap = Bitmap.CreateBitmap(
                    fullBitmap, 0, 0, fullBitmap.Width, fullBitmap.Height, matrix, false);
                fullBitmap.Recycle();

                await WriteBitmapToFile(finalPath, resizedBitmap);
            }

            File.Delete(selectedImage.Path);
            Java.IO.File newFile = new Java.IO.File(finalPath);
            return global::Android.Net.Uri.FromFile(newFile);
        }

        public static Type GetTaskCreationActivityType(string idName)
        {
            Type activityType;

            switch (idName)
            {
                case "INFO":
                    activityType = typeof(CreateTaskInfo);
                    break;
                case "MATCH_PHOTO":
                    activityType = typeof(CreateTaskPhotoMatch);
                    break;
                case "LOC_HUNT":
                    activityType = typeof(CreateTaskLocationHunt);
                    break;
                case "MAP_MARK":
                    activityType = typeof(CreateTaskLocationMarker);
                    break;
                case "MULT_CHOICE":
                    activityType = typeof(CreateTaskMultipleChoice);
                    break;
                case "DRAW_PHOTO":
                    activityType = typeof(CreateTaskDrawPhoto);
                    break;
                case "LISTEN_AUDIO":
                    activityType = typeof(CreateTaskListenAudio);
                    break;
                default:
                    activityType = typeof(CreateTaskBasic);
                    break;
            }

            return activityType;
        }


    }

    public sealed class OnDismissListener : Java.Lang.Object, IDialogInterfaceOnDismissListener
    {
        private readonly Action action;

        public OnDismissListener(Action action)
        {
            this.action = action;
        }

        public void OnDismiss(IDialogInterface dialog)
        {
            action();
        }
    }

    public class CompareSizesByArea : Java.Lang.Object, IComparator
    {
        public int Compare(Java.Lang.Object lhs, Java.Lang.Object rhs)
        {
            Size lhsSize = (Size)lhs;
            Size rhsSize = (Size)rhs;
            // We cast here to ensure the multiplications won't overflow
            return Long.Signum((long)lhsSize.Width * lhsSize.Height - (long)rhsSize.Width * rhsSize.Height);
        }
    }
}