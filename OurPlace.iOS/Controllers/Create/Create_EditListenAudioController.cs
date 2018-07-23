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

// This file has been autogenerated from a class added in the UI designer.

using System;
using System.IO;
using MobileCoreServices;
using Foundation;
using OurPlace.Common.Models;
using UIKit;
using System.Threading.Tasks;

namespace OurPlace.iOS
{
    public partial class Create_EditListenAudioController : Create_EditTaskController
    {
        private string previousFile;
        private string currentFile;
        private string listenEmpty = "No file chosen";
        private string listenValid = "Listen to Chosen Audio";
        private UIDocumentPickerViewController docPicker;
        private bool saved;

        public Create_EditListenAudioController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            ListenButton.TouchUpInside += ListenButton_TouchUpInside;
            ChooseAudioButton.TouchUpInside += ChooseAudioButton_TouchUpInside;
            ListenButton.SetTitle(listenEmpty, UIControlState.Disabled);
            ListenButton.SetTitle(listenValid, UIControlState.Normal);

            if (!string.IsNullOrWhiteSpace(thisTask?.JsonData))
            {
                previousFile = thisTask.JsonData;
                currentFile = previousFile;
                ListenButton.Enabled = true;
            }
            else
            {
                ListenButton.Enabled = false;
            }
        }

        private void ListenButton_TouchUpInside(object sender, EventArgs e)
        {
            string fullPath = AppUtils.GetPathForLocalFile(currentFile);

            if (!File.Exists(fullPath))
            {
                AppUtils.ShowSimpleDialog(this,
                                          "File Error",
                                          "There was an error reading the file. Please record a new one.",
                                          "Got it");
                return;
            }

            var tempTask = new AppTask
            {
                TaskType = thisTaskType,
                Description = thisTaskType.Description
            };

            ResultMediaViewerController listenAudioController = Storyboard.InstantiateViewController("MediaViewerController") as ResultMediaViewerController;
            listenAudioController.FilePath = fullPath;
            listenAudioController.Task = tempTask;
            listenAudioController.DeleteResult = null;
            NavigationController.PushViewController(listenAudioController, true);
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            if (IsMovingFromParentViewController)
            {
                // popped - if a file isn't being referenced by the saved data, delete it
                if (!saved && currentFile != null && currentFile != previousFile)
                {
                    string path = AppUtils.GetPathForLocalFile(currentFile);
                    File.Delete(path);
                    Console.WriteLine("Cleaned up file at " + path);
                }
            }
        }

        private void ChooseAudioButton_TouchUpInside(object sender, EventArgs e)
        {
            UIAlertController alert = UIAlertController.Create("Choose Image Source", null, UIAlertControllerStyle.ActionSheet);
            alert.AddAction(UIAlertAction.Create("Record New", UIAlertActionStyle.Default, (a) => { PerformSegue("CreateRecordAudio", this); }));
            alert.AddAction(UIAlertAction.Create("Choose Existing File", UIAlertActionStyle.Default, (a) => { OpenMediaPicker(); }));
            alert.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null));

            // On iPad, it's a pop up. Make it appear above the button
            UIPopoverPresentationController popCont = alert.PopoverPresentationController;
            if (popCont != null)
            {
                popCont.SourceView = ChooseAudioButton;
                popCont.SourceRect = ChooseAudioButton.Bounds;
                popCont.PermittedArrowDirections = UIPopoverArrowDirection.Down;
            }

            PresentViewController(alert, true, null);
        }

        private async Task OpenMediaPicker()
        {
            if (PresentedViewController != null)
            {
                await DismissViewControllerAsync(false);
            }

            // Allow the Document picker to select a range of document types
            var allowedUTIs = new string[] { UTType.Audio };

            // Display the picker
            docPicker = new UIDocumentPickerViewController(allowedUTIs, UIDocumentPickerMode.Import);
            docPicker.AllowsMultipleSelection = false;
            docPicker.DidPickDocumentAtUrls += DocPicker_DidPickDocumentAtUrls;

            await PresentViewControllerAsync(docPicker, false);
        }

        void DocPicker_DidPickDocumentAtUrls(object sender, UIDocumentPickedAtUrlsEventArgs e)
        {
            if (e.Urls == null || e.Urls.Length < 1) return;

            docPicker?.DismissViewControllerAsync(true);

            // IMPORTANT! You must lock the security scope before you can
            // access this file
            var securityEnabled = e.Urls[0].StartAccessingSecurityScopedResource();

            ThisApp.ClearDocumentHandler();
            ThisApp.DocumentLoaded += ThisApp_DocumentLoaded;
            ThisApp.OpenDocument(e.Urls[0]);

            // IMPORTANT! You must release the security lock established
            // above.
            e.Urls[0].StopAccessingSecurityScopedResource();
        }

        public void ThisApp_DocumentLoaded(Helpers.GenericTextDocument document)
        {
            if (currentFile != previousFile)
            {
                File.Delete(AppUtils.GetPathForLocalFile(currentFile));
            }

            string tempPath = document.FileUrl.Path;

            string folderPath = Common.LocalData.Storage.GetCacheFolder("created");
            currentFile = Path.Combine("created", DateTime.UtcNow.ToString("s") + Path.GetExtension(tempPath));

            string fullCurrentPath = AppUtils.GetPathForLocalFile(currentFile);

            File.Copy(tempPath, fullCurrentPath);

            ListenButton.Enabled = true;
        }


        public override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
        {
            base.PrepareForSegue(segue, sender);

            if (segue.Identifier.Equals("CreateRecordAudio"))
            {
                var viewController = (RecordAudioController)segue.DestinationViewController;
                viewController.createMode = true;
            }
        }

        // All activity editing controllers end up back here. 
        [Action("UnwindToCreateListenAudio:")]
        public void UnwindToLocationHunt(UIStoryboardSegue segue)
        {
            var sourceController = segue.SourceViewController as RecordAudioController;

            if (sourceController != null && !string.IsNullOrWhiteSpace(sourceController.innerPath))
            {

                if (!string.IsNullOrWhiteSpace(currentFile) && currentFile != previousFile)
                {
                    // the previous file was replaced without being saved, delete it
                    File.Delete(AppUtils.GetPathForLocalFile(currentFile));
                }

                currentFile = sourceController.innerPath;

                ListenButton.Enabled = true;
            }
        }

        protected override void FinishButton_TouchUpInside(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(previousFile) && string.IsNullOrWhiteSpace(currentFile))
            {
                AppUtils.ShowSimpleDialog(this, "Choose an Audio Recording", "Please select or record some audio for the user to listen to.", "Got it");
                return;
            }

            if (UpdateBasicTask())
            {
                if (string.IsNullOrWhiteSpace(currentFile))
                {
                    thisTask.JsonData = previousFile;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(previousFile) && currentFile != previousFile)
                    {
                        // new file replaces old one, delete the previous file
                        File.Delete(AppUtils.GetPathForLocalFile(previousFile));
                    }
                    thisTask.JsonData = currentFile;
                }

                UpdateActivity();
                saved = true;
                Unwind();
            }
        }
    }
}
