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
using UIKit;

namespace OurPlace.iOS.ViewSources
{
    public class SignInExplanationViewSource : UITableViewSource
    {
        private struct ExplanationStruct 
        {
            public string Title { get; set; }
            public string Description { get; set; }
        }

        private string descIdentifier = "LabelDescCell";
        private ExplanationStruct[] explanations = 
        { 
            new ExplanationStruct { 
                Title = "Why do I need to log into OurPlace through another service?", 
                Description = "OurPlace uses other social media services to authenticate user accounts through a secure process called OAuth. This lets our users create and sign into accounts, without needing to think up and remember yet another password. It also means you don\'t have to trust us with keeping any passwords safe - we never receive one! You just sign in with a service you likely already have an account for. This makes things easier and more secure for both of us."},
            new ExplanationStruct { 
                Title = "What information does OurPlace take from these services?", 
                Description = "We take the minimum amount of information from your social media account in order to provide a good service. We don\'t have access to your friends lists, and we can\'t and won\'t post things on your behalf. We simply store your name, date of birth, a link to your profile picture and your email address."},
            new ExplanationStruct { 
                Title = "Why do you need this information and how will it be used?", 
                Description = "We need an email address to be able to identify you across services. If, for example, you signed into OurPlace with a Google account and then the next week signed in using Facebook, we could see that you\'re the same person thanks to the matching email address. We will not share any of your details with third parties and we will not spam your email account. The other basic profile information is used to personalise your OurPlace experience and help us keep track of how the service is being used."},
            
        
        };

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            ExplanationCell cell = (ExplanationCell)tableView.DequeueReusableCell(ExplanationCell.Key, indexPath);

            string thisString = explanations[indexPath.Section].Description;
            cell.UpdateContent(thisString);

            return cell;
        }

		public override string TitleForHeader(UITableView tableView, nint section)
		{
            return explanations[section].Title;
		}

		public override nint NumberOfSections(UITableView tableView)
		{
            return explanations.Length;
		}

		public override nint RowsInSection(UITableView tableview, nint section)
        {
            return 1;
        }
    }
}
