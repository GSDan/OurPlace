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
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Android.Adapters;
using OurPlace.Common.LocalData;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OurPlace.Common;

namespace OurPlace.Android.Activities.Create
{
    [Activity(Label = "Upload Queue", Theme = "@style/OurPlaceActionBar", ParentActivity = typeof(MainActivity))]
    public class UploadsActivity : AppCompatActivity
    {
        RecyclerView recyclerView;
        RecyclerView.LayoutManager layoutManager;
        UploadsAdapter adapter;
        List<AppDataUpload> uploads;
        List<FileUpload> files;
        TextView headerText;
        DatabaseManager dbManager;
        ProgressDialog uploadProgress;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.UploadsActivity);

            headerText = FindViewById<TextView>(Resource.Id.uploadsHeaderMessage);

            LoadQueue();
        }

        private async Task LoadQueue()
        {
            dbManager = await Storage.GetDatabaseManager();

            uploads = dbManager.GetUploadQueue().ToList();

            if (uploads.Count > 0)
            {
                headerText.SetText(Resource.String.uploadsSome);
            }

            adapter = new UploadsAdapter(this, uploads);
            adapter.UploadClick += UploadAfterWarning;
            adapter.DeleteClick += DeleteClick;

            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            recyclerView.SetAdapter(adapter);

            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);
        }

        private void HandleProgress(long bytes, long totalBytes, long totalBytesExpected)
        {
            Console.WriteLine("Downloading {0}/{1}", totalBytes, totalBytesExpected);
            if (uploadProgress == null || !uploadProgress.IsShowing) return;

            RunOnUiThread(() =>
            {
                uploadProgress.Max = 10000;
                var progressPercent = (float)totalBytes / (float)totalBytesExpected;
                var progressOffset = Convert.ToInt32(progressPercent * 10000);
                Console.WriteLine(progressOffset);
                uploadProgress.Progress = progressOffset;
            });
        }

        private void DeleteClick(object sender, int e)
        {
            new global::Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(Resource.String.deleteTitle)
                .SetMessage(Resource.String.deleteMessage)
                .SetNegativeButton(Resource.String.dialog_cancel, (a, b) => { })
                .SetCancelable(true)
                .SetPositiveButton(Resource.String.DeleteBtn, (a, b) =>
                {
                    DeleteFiles(adapter.data[e]);
                    dbManager.DeleteUpload(adapter.data[e]);
                    adapter.data.RemoveAt(e);
                    adapter.NotifyDataSetChanged();
                })
                .Show();
        }

        private void DeleteFiles(AppDataUpload data)
        {
            files = JsonConvert.DeserializeObject<List<FileUpload>>(data.FilesJson);
            foreach (FileUpload up in files)
            {
                if (File.Exists(up.LocalFilePath))
                {
                    File.Delete(up.LocalFilePath);
                }
            }
        }

        /// <summary>
        /// Warns the user prior to starting upload if file sizes are significant
        /// </summary>
        private void UploadAfterWarning(object sender, int position)
        {
            files = JsonConvert.DeserializeObject<List<FileUpload>>(uploads[position].FilesJson);
            float totalFileSizeMb = 0;

            foreach (FileUpload up in files)
            {
                if (!string.IsNullOrWhiteSpace(up.RemoteFilePath))
                {
                    continue;
                }

                FileInfo fInfo = new FileInfo(up.LocalFilePath);
                if (fInfo.Exists)
                {
                    totalFileSizeMb += fInfo.Length / 1000000f;
                }
            }

            if (totalFileSizeMb > 10)
            {
                string unit = (totalFileSizeMb > 1000) ? "GB" : "MB";
                float amount = (totalFileSizeMb > 1000) ? totalFileSizeMb / 1000 : totalFileSizeMb;

                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.uploadsSizeWarningTitle)
                    .SetMessage(string.Format(base.Resources.GetString(Resource.String.uploadsSizeWarningMessage), amount.ToString("0.0"), unit))
                    .SetNegativeButton(Resource.String.dialog_cancel, (a, b) => { })
                    .SetCancelable(true)
                    .SetPositiveButton(Resource.String.Continue, (a, b) =>
                    {
                        StartUploads(position);
                    })
                    .Show();
            }
            else
            {
                StartUploads(position);
            }
        }

        private async void StartUploads(int position)
        {
            uploadProgress = new ProgressDialog(this);
            uploadProgress.SetTitle("Uploading");
            uploadProgress.Indeterminate = false;
            uploadProgress.SetCancelable(false);
            uploadProgress.Max = 100;
            uploadProgress.Progress = 0;
            uploadProgress.SetTitle(Resource.String.uploadsUploadingFiles);
            uploadProgress.Show();

            try
            {
                bool success = await Storage.UploadFiles(
                    JsonConvert.DeserializeObject<List<FileUpload>>(
                        uploads[position].FilesJson),
                        position,
                        (percentage) =>
                        {
                            uploadProgress.Progress = percentage;
                        },
                        (listPos, jsonData) =>
                        {
                            uploads[listPos].FilesJson = jsonData;
                            dbManager.UpdateUpload(uploads[listPos]);
                        },
                    Storage.GetUploadsFolder());

                if (!success)
                {
                    uploadProgress.Dismiss();
                    new global::Android.Support.V7.App.AlertDialog.Builder(this)
                        .SetTitle(Resource.String.ErrorTitle)
                        .SetMessage(Resource.String.ConnectionError)
                        .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                        .Show();
                    return;
                }
            }
            catch (Exception e)
            {
                // Refresh token not valid
                Console.WriteLine(e.Message);
                var suppress = AndroidUtils.ReturnToSignIn(this);
                Toast.MakeText(this, Resource.String.ForceSignOut, ToastLength.Long).Show();
                return;
            }

            ServerResponse<string> resp = new ServerResponse<string>();

            if (uploads[position].UploadType == UploadType.NewActivity)
            {
                resp = await ServerUtils.UploadNewActivity(uploads[position]);
            }
            else
            {
                // Uploading activity results
                AppTask[] results = JsonConvert.DeserializeObject<AppTask[]>(uploads[position].JsonData) ?? new AppTask[0];
                files = JsonConvert.DeserializeObject<List<FileUpload>>(uploads[position].FilesJson);
                resp = await ServerUtils.UpdateAndPostResults(results, files, uploads[position].UploadRoute);
            }

            uploadProgress.Dismiss();

            if (resp == null)
            {
                var suppress = AndroidUtils.ReturnToSignIn(this);
                Toast.MakeText(this, Resource.String.ForceSignOut, ToastLength.Long).Show();
                return;
            }

            if (!resp.Success)
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.ErrorTitle)
                    .SetMessage(resp.Message)
                    .SetPositiveButton(Resource.String.dialog_ok, (a, b) => { })
                    .Show();
                return;
            }

            dbManager.DeleteUpload(uploads[position]);
            adapter.data.RemoveAt(position);
            adapter.NotifyDataSetChanged();

            new global::Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(Resource.String.uploadsUploadSuccessTitle)
                .SetMessage(Resource.String.uploadsUploadSuccessMessage)
                .SetPositiveButton(Resource.String.dialog_ok, (a, b) =>
                {
                    if (adapter.data.Count == 0)
                    {
                        OnBackPressed();
                    }
                })
                .Show();
        }

        public override void OnBackPressed()
        {
            Intent toMainIntent = new Intent(this, typeof(MainActivity));
            toMainIntent.AddFlags(ActivityFlags.ClearTop);
            toMainIntent.AddFlags(ActivityFlags.SingleTop);
            StartActivity(toMainIntent);
            Finish();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] global::Android.App.Result resultCode, Intent data)
        {

        }
    }
}