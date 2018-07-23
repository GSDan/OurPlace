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

namespace OurPlace.iOS.Helpers
{
    // For testing with DocumentPicker https://docs.microsoft.com/en-us/xamarin/ios/platform/document-picker
    public class GenericTextDocument : UIDocument
    {
        #region Private Variable Storage
        private NSString _dataModel;
        #endregion

        #region Computed Properties
        public string Contents
        {
            get { return _dataModel.ToString(); }
            set { _dataModel = new NSString(value); }
        }
        #endregion

        #region Constructors
        public GenericTextDocument(NSUrl url) : base(url)
        {
            // Set the default document text
            this.Contents = "";
        }

        public GenericTextDocument(NSUrl url, string contents) : base(url)
        {
            // Set the default document text
            this.Contents = contents;
        }
        #endregion

        #region Override Methods
        public override bool LoadFromContents(NSObject contents, string typeName, out NSError outError)
        {
            // Clear the error state
            outError = null;

            // Were any contents passed to the document?
            if (contents != null)
            {
                _dataModel = NSString.FromData((NSData)contents, NSStringEncoding.UTF8);
            }

            // Inform caller that the document has been modified
            RaiseDocumentModified(this);

            // Return success
            return true;
        }

        public override NSObject ContentsForType(string typeName, out NSError outError)
        {
            // Clear the error state
            outError = null;

            // Convert the contents to a NSData object and return it
            NSData docData = _dataModel?.Encode(NSStringEncoding.UTF8);
            return docData;
        }
        #endregion

        public override bool WriteContents(NSObject contents, NSUrl toUrl, UIDocumentSaveOperation saveOperation, NSUrl originalContentsURL, out NSError outError)
        {
            if (contents != null)
            {
                Console.WriteLine("IN WRITE CONTENTS");
                return base.WriteContents(contents, toUrl, saveOperation, originalContentsURL, out outError);
            }
            outError = new NSError(new NSString("CONTENTS NULL"), 999);
            return false;
        }

        #region Events
        public delegate void DocumentModifiedDelegate(GenericTextDocument document);
        public event DocumentModifiedDelegate DocumentModified;

        internal void RaiseDocumentModified(GenericTextDocument document)
        {
            // Inform caller
            if (this.DocumentModified != null)
            {
                this.DocumentModified(document);
            }
        }
        #endregion
    }
}
