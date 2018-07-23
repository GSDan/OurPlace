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
using System.Threading.Tasks;
using CoreLocation;
using Foundation;
using GlobalToast;
using Newtonsoft.Json;
using OurPlace.iOS.Cells;
using OurPlace.iOS.Delegates;
using OurPlace.iOS.Helpers;
using OurPlace.iOS.ViewSources;
using OurPlace.Common.LocalData;
using OurPlace.Common.Models;
using UIKit;

namespace OurPlace.iOS
{
	public partial class MainHighlightsController : UIViewController
    {
        private ActivityViewSource source;
		private CLLocationManager locationManager;
		private CLLocation lastLoc;
		private UIRefreshControl refreshControl;
		private ActivityFeedSection recentActivities;
		private List<ActivityFeedSection> lastFromServer;
		private DatabaseManager dbManager;

        public MainHighlightsController (IntPtr handle) : base (handle)
        {

        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            if(TabBarController != null)
            {
                TabBarController.NavigationItem.Title = "Highlights";

                TabBarController.NavigationItem.LeftBarButtonItem = new UIBarButtonItem(
                    UIImage.FromBundle("ScanIcon"),
                    UIBarButtonItemStyle.Plain,
                    GetScan);

                TabBarController.NavigationItem.RightBarButtonItem = new UIBarButtonItem(
                        UIImage.FromBundle("SearchIcon"),
                        UIBarButtonItemStyle.Plain,
                        SearchForActivity);
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            collectionView.RegisterNibForCell(ActivityCollectionCell.Nib, ActivityCollectionCell.Key);
            collectionView.RegisterNibForSupplementaryView(FeedSectionHeader.Nib, UICollectionElementKindSectionKey.Header, FeedSectionHeader.Key);

			locationManager = new CLLocationManager();
			locationManager.DesiredAccuracy = CLLocation.AccuracyHundredMeters;
            locationManager.LocationsUpdated += LocationUpdated;

            source = new ActivityViewSource();
            
			refreshControl = new UIRefreshControl();
			refreshControl.TintColor = AppUtils.AppMainColour;
			refreshControl.ValueChanged += (sender, e) =>
			{
				RefreshFeed();
			};

            collectionView.ShowsHorizontalScrollIndicator = false;
			collectionView.RefreshControl = refreshControl;
            collectionView.Source = source;
            collectionView.AllowsSelection = true;
            collectionView.Delegate = new SectionedClickableDelegate((section, index) => { 
				var suppressAsync = AppUtils.OpenActivity(source.Rows[section]?.Activities[index], Storyboard, NavigationController);
            });
		}

		private void LocationUpdated(object sender, CLLocationsUpdatedEventArgs e)
        {
            if (!(e?.Locations?.Length > 0))
            {
                return;
            }
            
			lastLoc = e.Locations[e.Locations.Length - 1];         
			var suppressAsync = GetFromServer();
        }

		public override void ViewDidAppear(bool animated)
		{
			base.ViewDidAppear(animated);

			collectionView.ContentOffset = new CoreGraphics.CGPoint(0, -refreshControl.Frame.Size.Height);

			RefreshFeed();
		}

        private async void RefreshFeed()
		{         
			ShowLoading();

			if(dbManager == null)
			{
				dbManager = await Storage.GetDatabaseManager();
			}         

			recentActivities = LoadRecent();
			if(recentActivities != null)
			{
				source.Rows = new List<ActivityFeedSection> { recentActivities };
				collectionView.ReloadData();
			}

			// If we don't have internet, don't bother getting the location or 
            // polling the server
            if (!AppDelegate.Online)
            {
                Console.WriteLine("No Internet access, loading cached feed");
                Toast.ShowToast("Couldn't reach the server - please check your connection!");
                AppDelegate.WhenOnline = RefreshFeed; // Reload if connection returns
                return;
            }
         
			ShowLoading();
           
			// check is location is enabled
            if(CLLocationManager.LocationServicesEnabled)
            {
                // check location permission
                if (CLLocationManager.Status == CLAuthorizationStatus.NotDetermined)
                {
                    AppUtils.ShowSimpleDialog(
                        this,
                        "Show nearby activities?",
                        "Please allow OurPlace location authorization if you would like to see nearby activities.",
                        "Got it",
                        (UIAlertAction action) =>
                        {
                            RequestLocation();
                        });
                }
                else if (CLLocationManager.Status == CLAuthorizationStatus.Denied)
                {
                    var suppress = GetFromServer();
                }
                else
                {
                    RequestLocation();
                }
            }
            else
            {
				var suppress = GetFromServer();
            }         
		}

        private void RequestLocation()
		{
			ShowLoading();
			locationManager.RequestWhenInUseAuthorization();
            locationManager.RequestLocation();
		}

        private void ShowLoading()
		{
			if(!refreshControl.Refreshing)
			{
				refreshControl.BeginRefreshing();
			}
		}

        private void HideLoading()
		{
			if(refreshControl.Refreshing)
			{
				refreshControl.EndRefreshing();
                if(collectionView.NumberOfSections() > 0)
                {
                    collectionView.ScrollToItem(NSIndexPath.FromRowSection(0, 0), UICollectionViewScrollPosition.Bottom, true);
                }
			}
		}

		private ActivityFeedSection LoadRecent()
		{
            List<LearningActivity> recentlyOpened = dbManager.GetActivities();

			if (recentlyOpened != null && recentlyOpened.Count > 0)
            {
                return new ActivityFeedSection
                {
                    Title = "Your Recent Activities",
                    Description = "You've opened these activities recently",
                    Activities = recentlyOpened
                };
            }
			return null;
		}

		private async Task GetFromServer()
		{
			ShowLoading();

			double lat = 0;
			double lon = 0;
            
			if(lastLoc != null)
			{
				lat = lastLoc.Coordinate.Latitude;
				lon = lastLoc.Coordinate.Longitude;
			}
         
            Common.ServerResponse<List<ActivityFeedSection>> remoteResults = await Common.ServerUtils.Get<List<ActivityFeedSection>>(
				string.Format("/api/learningactivities/GetFeed?lat={0}&lon={1}", lat, lon));

			HideLoading();

            if(remoteResults == null)
            {
                var suppress = AppUtils.SignOut(this);
            }

			List<ActivityFeedSection> feed = new List<ActivityFeedSection>();
            
            // add already cached section
			if(recentActivities != null)
			{
				feed.Add(recentActivities);
			}

            if (remoteResults.Success && remoteResults.Data != null)
            {
            	lastFromServer = remoteResults.Data;
            
            	// Save this in the offline cache
            	dbManager.currentUser.CachedActivitiesJson = JsonConvert.SerializeObject(remoteResults.Data,
            		new JsonSerializerSettings
            		{
            			TypeNameHandling = TypeNameHandling.Objects,
            			ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            			MaxDepth = 6
            		});
            	dbManager.AddUser(dbManager.currentUser);
            }
            else
            {
            	Toast.ShowToast("Couldn't reach the server - please check your connection!");
                lastFromServer = JsonConvert.DeserializeObject<List<ActivityFeedSection>>(
                    dbManager.currentUser.CachedActivitiesJson);
            }
        
			if(lastFromServer != null) feed.AddRange(lastFromServer);
                     
			source.Rows = feed;
           
			collectionView.ReloadData();
		}

        private async void GetScan(object s, object e)
        {
            ZXing.Mobile.MobileBarcodeScanner scanner = new ZXing.Mobile.MobileBarcodeScanner();
            ZXing.Result result = await scanner.Scan();

            if (result == null) return;

            Console.WriteLine("Scanned Barcode: " + result.Text);

            NSUrlComponents comps = NSUrlComponents.FromString(result.Text);
            if (comps != null && comps.QueryItems != null)
            {
                foreach (NSUrlQueryItem queryItem in comps.QueryItems)
                {
                    if (queryItem.Name == "code")
                    {
                        var suppress = GetAndOpenActivity(queryItem.Value);
                        return;
                    }
                }
            }

            AppUtils.ShowSimpleDialog(this, "Not found", "Are you sure that was a valid OurPlace QR code?", "Got it");
        }

        private void SearchForActivity(object s, object e)
        {
            UIAlertController alert = UIAlertController.Create("Enter Share Code",
                                                               "Enter the share code for the activity you wish to open.",
                                                               UIAlertControllerStyle.Alert);
            alert.AddTextField((textField) =>
            {
                textField.Placeholder = "Share code";
            });

            alert.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null));
            alert.AddAction(UIAlertAction.Create("Search", UIAlertActionStyle.Default, (UIAlertAction obj) =>
            {
                string enteredText = alert.TextFields[0].Text;
                if (string.IsNullOrWhiteSpace(enteredText)) return;

                var suppress = GetAndOpenActivity(enteredText);
            }));

            PresentViewController(alert, true, null);
        }

        private async Task GetAndOpenActivity(string code)
        {
            LoadingOverlay loadPop = new LoadingOverlay(UIScreen.MainScreen.Bounds);
            View.Add(loadPop);

            Common.ServerResponse<LearningActivity> result =
                await Common.ServerUtils.Get<LearningActivity>("/api/LearningActivities/GetWithCode?code=" + code);

            loadPop.Hide();

            if (result == null)
            {
                var suppress = AppUtils.SignOut(this);
            }

            if (result.Success)
            {
                var suppress = AppUtils.OpenActivity(result.Data, Storyboard, NavigationController);
            }
            else
            {
                if (result.Message.StartsWith("404"))
                {
                    AppUtils.ShowSimpleDialog(this, "Not found", "No activity was found with that share code.", "Got it");
                }
                else
                {
                    AppUtils.ShowSimpleDialog(this, "Connection Error", "Something went wrong! Please try again later.", "Got it");
                }
            }
        }
    }
}