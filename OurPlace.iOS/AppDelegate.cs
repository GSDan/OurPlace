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
using Foundation;
using OurPlace.iOS.Helpers;
using OurPlace.Common;
using UIKit;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ObjCRuntime;
using System.IO;
using Google.Places;

namespace OurPlace.iOS
{
    // Document Picker code from
    // https://docs.microsoft.com/en-us/xamarin/ios/platform/document-picker

    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        public const string TestFilename = "test.txt";

        public override UIWindow Window { get; set; }
        public bool HasiCloud { get; set; }
        public bool CheckingForiCloud { get; set; }
        public NSUrl iCloudUrl { get; set; }

        public GenericTextDocument Document { get; set; }
        public NSMetadataQuery Query { get; set; }
        public NSData Bookmark { get; set; }

        public static bool IsInDesignerView = true;
        public static bool Online;
        public static Action WhenOnline;

        public delegate void DocumentLoadedDelegate(GenericTextDocument document);
        public event DocumentLoadedDelegate DocumentLoaded;

        internal void RaiseDocumentLoaded(GenericTextDocument document)
        {
            // Inform caller
            DocumentLoaded?.Invoke(document);
        }

        public void ClearDocumentHandler()
        {
            DocumentLoaded = null;
        }

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            IsInDesignerView = false;
            PlacesClient.ProvideApiKey(ConfidentialData.mapsk);
            Google.Maps.MapServices.ProvideAPIKey(ConfidentialData.mapsk);
            UINavigationBar.Appearance.TintColor = AppUtils.AppAltColour;
            UINavigationBar.Appearance.BackgroundColor = AppUtils.AppMainColour;//.ColorWithAlpha(0.8f);
            UINavigationBar.Appearance.Translucent = true;


            Online = Reachability.RemoteHostStatus() != NetworkStatus.NotReachable;

            Reachability.ReachabilityChanged += delegate
            {
                Online = Reachability.RemoteHostStatus() != NetworkStatus.NotReachable;
                if (Online)
                {
                    WhenOnline?.Invoke();
                    WhenOnline = null;
                }
                Console.WriteLine("Network change - Online: " + Online);
            };

            // Start a new thread to check and see if the user has iCloud
            // enabled.
            new Thread(new ThreadStart(() =>
            {
                // Inform caller that we are checking for iCloud
                CheckingForiCloud = true;

                // Checks to see if the user of this device has iCloud
                // enabled
                var uburl = NSFileManager.DefaultManager.GetUrlForUbiquityContainer(null);

                // Connected to iCloud?
                if (uburl == null)
                {
                    // No, inform caller
                    HasiCloud = false;
                    iCloudUrl = null;
                    Console.WriteLine("Unable to connect to iCloud");
                }
                else
                {
                    // Yes, inform caller and save location the the Application Container
                    HasiCloud = true;
                    iCloudUrl = uburl;
                    Console.WriteLine("Connected to iCloud");

                    // If we have made the connection with iCloud, start looking for documents
                    //InvokeOnMainThread(FindDocument);
                }

                // Inform caller that we are no longer looking for iCloud
                CheckingForiCloud = false;

            })).Start();

            return true;
        }

        public override void OnResignActivation(UIApplication application)
        {
            // Invoked when the application is about to move from active to inactive state.
            // This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) 
            // or when the user quits the application and it begins the transition to the background state.
            // Games should use this method to pause the game.
        }

        public override void DidEnterBackground(UIApplication application)
        {
            // Use this method to release shared resources, save user data, invalidate timers and store the application state.
            // If your application supports background exection this method is called instead of WillTerminate when the user quits.

            // Trap all errors
            try
            {
                // Values to include in the bookmark packet
                var resources = new string[] {
                    NSUrl.FileSecurityKey,
                    NSUrl.ContentModificationDateKey,
                    NSUrl.FileResourceIdentifierKey,
                    NSUrl.FileResourceTypeKey,
                    NSUrl.LocalizedNameKey
                };

                // Create the bookmark
                NSError err;
                Bookmark = Document.FileUrl.CreateBookmarkData(NSUrlBookmarkCreationOptions.WithSecurityScope, resources, iCloudUrl, out err);

                // Was there an error?
                if (err != null)
                {
                    // Yes, report it
                    Console.WriteLine("Error Creating Bookmark: {0}", err.LocalizedDescription);
                }
            }
            catch (Exception e)
            {
                // Report error
                Console.WriteLine("Error: {0}", e.Message);
            }
        }

        public override void WillEnterForeground(UIApplication application)
        {
            // Called as part of the transiton from background to active state.
            // Here you can undo many of the changes made on entering the background.

            // Is there any bookmark data?
            if (Bookmark != null)
            {
                // Trap all errors
                try
                {
                    // Yes, attempt to restore it
                    bool isBookmarkStale;
                    NSError err;
                    var srcUrl = new NSUrl(Bookmark, NSUrlBookmarkResolutionOptions.WithSecurityScope, iCloudUrl, out isBookmarkStale, out err);

                    // Was there an error?
                    if (err != null)
                    {
                        // Yes, report it
                        Console.WriteLine("Error Loading Bookmark: {0}", err.LocalizedDescription);
                    }
                    else
                    {
                        // Load document from bookmark
                        OpenDocument(srcUrl);
                    }
                }
                catch (Exception e)
                {
                    // Report error
                    Console.WriteLine("Error: {0}", e.Message);
                }
            }
        }

        public override void OnActivated(UIApplication application)
        {
            // Restart any tasks that were paused (or not yet started) while the application was inactive. 
            // If the application was previously in the background, optionally refresh the user interface.
            application.KeyWindow.TintColor = AppUtils.AppAltColour;
        }

        public override void WillTerminate(UIApplication application)
        {
            // Called when the application is about to terminate. Save data, if needed. See also DidEnterBackground.
        }

        public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            if (sourceApplication == "com.apple.SafariViewService")
            {
                NSMutableDictionary dict = new NSMutableDictionary();
                dict.Add(new NSString("url"), url);

                NSNotificationCenter.DefaultCenter.PostNotificationName("ParkLearnLoginCallback", this, dict);
                return true;
            }

            return false;
        }

        private void FindDocument()
        {
            Console.WriteLine("Finding Document...");

            // Create a new query and set it's scope
            Query = new NSMetadataQuery();
            Query.SearchScopes = new NSObject[] {
                NSMetadataQuery.UbiquitousDocumentsScope,
                NSMetadataQuery.UbiquitousDataScope,
                NSMetadataQuery.AccessibleUbiquitousExternalDocumentsScope
            };

            // Build a predicate to locate the file by name and attach it to the query
            var pred = NSPredicate.FromFormat("%K == %@",
                 new NSObject[] {NSMetadataQuery.ItemFSNameKey
                , new NSString(TestFilename)});
            Query.Predicate = pred;

            // Register a notification for when the query returns
            NSNotificationCenter.DefaultCenter.AddObserver(this
                , new Selector("queryDidFinishGathering:")
                , NSMetadataQuery.DidFinishGatheringNotification
                , Query);

            // Start looking for the file
            Query.StartQuery();
            Console.WriteLine("Querying: {0}", Query.IsGathering);
        }

        [Export("queryDidFinishGathering:")]
        public void DidFinishGathering(NSNotification notification)
        {
            Console.WriteLine("Finish Gathering Documents.");

            // Access the query and stop it from running
            var query = (NSMetadataQuery)notification.Object;
            query.DisableUpdates();
            query.StopQuery();

            // Release the notification
            NSNotificationCenter.DefaultCenter.RemoveObserver(this
                , NSMetadataQuery.DidFinishGatheringNotification
                , query);

            // Load the document that the query returned
            LoadDocument(query);
        }

        private void LoadDocument(NSMetadataQuery query)
        {
            Console.WriteLine("Loading Document...");

            // Take action based on the returned record count
            switch (query.ResultCount)
            {
                case 0:
                    // Create a new document
                    CreateNewDocument();
                    break;
                case 1:
                    // Gain access to the url and create a new document from
                    // that instance
                    NSMetadataItem item = (NSMetadataItem)query.ResultAtIndex(0);
                    var url = (NSUrl)item.ValueForAttribute(NSMetadataQuery.ItemURLKey);

                    // Load the document
                    OpenDocument(url);
                    break;
                default:
                    // There has been an issue
                    Console.WriteLine("Issue: More than one document found...");
                    break;
            }
        }

        public void OpenDocument(NSUrl url)
        {
            try
            {
                Console.WriteLine("Attempting to open: {0}", url);
                Document = new GenericTextDocument(url);

                // Open the document
                Document.Open((success) =>
                {
                    if (success)
                    {
                        Console.WriteLine("Document Opened");
                    }
                    else
                        Console.WriteLine("Failed to Open Document");
                });

                // Inform caller
                RaiseDocumentLoaded(Document);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        public void CreateNewDocument()
        {
            // Create path to new file
            // var docsFolder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            var docsFolder = Path.Combine(iCloudUrl.Path, "Documents");
            var docPath = Path.Combine(docsFolder, TestFilename);
            var ubiq = new NSUrl(docPath, false);

            // Create new document at path 
            Console.WriteLine("Creating Document at:" + ubiq.AbsoluteString);
            Document = new GenericTextDocument(ubiq);

            // Set the default value
            Document.Contents = "(default value)";

            // Save document to path
            Document.Save(Document.FileUrl, UIDocumentSaveOperation.ForCreating, (saveSuccess) =>
            {
                Console.WriteLine("Save completion:" + saveSuccess);
                if (saveSuccess)
                {
                    Console.WriteLine("Document Saved");
                }
                else
                {
                    Console.WriteLine("Unable to Save Document");
                }
            });

            // Inform caller
            RaiseDocumentLoaded(Document);
        }

        public bool SaveDocument()
        {
            bool successful = false;

            Console.WriteLine("in save document (AppDelegate)");

            // Save document to path
            Document.Save(Document.FileUrl, UIDocumentSaveOperation.ForOverwriting, (saveSuccess) =>
            {
                Console.WriteLine("Save completion: " + saveSuccess);
                if (saveSuccess)
                {
                    Console.WriteLine("Document Saved");
                    successful = true;
                }
                else
                {
                    Console.WriteLine("Unable to Save Document");
                    successful = false;
                }
            });

            // Return results
            return successful;
        }
    }
}

