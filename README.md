<p align="center">
	<img src="https://raw.githubusercontent.com/GSDan/OurPlace/master/Media/StoreBanner.png" width="800" align="center">
</p>

<p align="center">
	OurPlace is a mobile learning platform, designed to support communities in creating and sharing interactive learning activities about the places they care most about.
	<br><br>
	<p align="center">
	<img src="https://raw.githubusercontent.com/GSDan/OurPlace/master/Media/screenshotDevice.png" height="500" align="center">
	<img src="https://raw.githubusercontent.com/GSDan/OurPlace/master/Media/iphonexspacegrey_portrait.png" height="500" align="center">
</p>

<p align="center">
	<br>
	<a href="https://ourplace.app">Visit the OurPlace website</a>
	<br><br>
    <a href="https://itunes.apple.com/us/app/ourplace/id1378985779?ls=1&amp;mt=8">
        <img alt="Get it on the App Store" src="https://ourplace.app/Content/img/icons/appStore.svg" height="50">
    </a>
    <a href="https://play.google.com/store/apps/details?id=com.park.learn&amp;utm_source=Website-GettingStarted&amp;pcampaignid=MKT-Other-global-all-co-prtnr-py-PartBadge-Mar2515-1">
        <img alt="Get it on Google Play" src="https://ourplace.app/Content/img/icons/googlePlayBadge.png" height="50">
    </a>
</p>


## What is OurPlace?
OurPlace is a mobile platform which supports the creation, sharing and completion of highly customisable mobile learning activities. These activities are built by combining together bite-size modular tasks, which can each ask the user to perform a particular action. Tasks can ask the learner to take photos or video, record an audio clip, listen to a given audio clip, match an existing image (by comparing to an image overlay), draw a picture, draw on top of an existing image, mark locations on a map, navigate to a given location, answer a multiple choice question or simply read a piece of text.

This functionality is further expanded by supporting ‘follow-up’ tasks, which become available to the learner once another task has been completed. For example, an activity might ask the learner to walk to a particular location where, upon arrival, follow-up tasks ask them to document their thoughts through photos and an audio recording.	Once created, activities can be shared in numerous ways. The accompanying website supplies QR codes which can be printed and launch the activity when scanned. Users can also enter given share codes, or launch activities when nearby the location activities have been optionally tagged with.

## Project Structure
The project's Visual Studio solution *(OurPlace.sln)* is split into four projects: *OurPlace.Common*, *OurPlace.API*, *OurPlace.Android* and *OurPlace.iOS*.

### OurPlace.Common
This is a .NET Standard 2.0 project which serves the other projects within the solution with shared data models, interfaces and common functionality. Static classes cater to the iOS and Android projects, with functions for local database management, file storage and REST request logic.
***A file named 'ConfidentialData.cs', containing information such as the API's address, has been omitted from this repository and needs to be replicated for OurPlace.Common to successfully build.***

### OurPlace.API
This project uses version 4.6.1 of the .NET Framework, and contains both a ASP .NET website, a Web API 2 powered API and Code First, Entity Framework database. The API and website share user accounts, which are handled by external OAuth services via OWIN.
***Two config files containing database connection strings and OAuth service details have been omitted from this repository and need to be replicated for OurPlace.API to successfully build.***

### OurPlace.Android & OurPlace.iOS
These projects use Xamarin.Android and Xamarin.iOS to produce native mobile applications using a C# codebase. While much of the core logic is shared through reference to OurPlace.Common, both applications contain large amounts of platform-specific code which deals with GUI interactions and native functionality.
***Please note that a machine running OSX is required to build OurPlace.iOS***
	

> I created OurPlace as a part of my [EPSRC funded](http://gow.epsrc.ac.uk/NGBOViewGrant.aspx?GrantRef=EP/L016176/1) [Digital Civics](https://digitalcivics.io/) PhD at Open Lab.<br>It is free to use under the GNU General Public License v3.0, a copy of which can be found in this repository.


<p align="center">
	<img src="http://s3.amazonaws.com/libapps/accounts/21667/images/epsrc-lowres.jpg" height="100" align="center">
	<img src="http://indigomultimedia.com/wp-content/uploads/2016/11/dc-dark.svg" height="150" align="center">
	<img src="http://www.collectionsdivetwmuseums.org.uk/img/logos/ncl-light.jpeg" height="100" align="center">
</p>
