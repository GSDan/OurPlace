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
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.Facebook;
using Microsoft.Owin.Security.OAuth;
using Owin.Security.Providers.OpenID;
using Owin;
using OurPlace.API.Models;
using OurPlace.API.Providers;
using System;
using System.Security.Claims;
using System.Net.Http;
using ParkLearn.API.Providers;
using System.Configuration;
using Owin.Security.Providers.OpenIDBase;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Net;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using Microsoft.Owin.Security.Notifications;
using IdentityModel.Client;

namespace OurPlace.API
{
    public partial class Startup
    {
        public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }

        public static string PublicClientId { get; private set; }

        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Configure the db context and user manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Configure the application for OAuth based flow
            PublicClientId = "self";
            OAuthOptions = new OAuthAuthorizationServerOptions
            {
                TokenEndpointPath = new PathString("/Token"),
                Provider = new ApplicationOAuthProvider(PublicClientId),
                AuthorizeEndpointPath = new PathString("/api/Account/ExternalLogin"),
                AccessTokenExpireTimeSpan = TimeSpan.FromHours(1),
                RefreshTokenProvider = new RefreshTokenProvider(),
                // In production mode set AllowInsecureHttp = false
                AllowInsecureHttp = false
            };

            // Enable the application to use bearer tokens to authenticate users
            app.UseOAuthBearerTokens(OAuthOptions);

            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseTwitterAuthentication(
            //    consumerKey: "",
            //    consumerSecret: "");

            var facebookAuthOptions = new FacebookAuthenticationOptions
            {
                AppId = ConfigurationManager.AppSettings["oauth:facebook:id"],
                AppSecret = ConfigurationManager.AppSettings["oauth:facebook:secret"],
                Provider = new FacebookAuthProvider()
            };

            facebookAuthOptions.Scope.Add("email");
            facebookAuthOptions.Scope.Add("public_profile");

            app.UseFacebookAuthentication(facebookAuthOptions);

            var googleAuthOptions = new GoogleOAuth2AuthenticationOptions()
            {
                ClientId = ConfigurationManager.AppSettings["oauth:google:id"],
                ClientSecret = ConfigurationManager.AppSettings["oauth:google:secret"],
                
                Provider = new GoogleOAuth2AuthenticationProvider()
                {
                    // Force the account choose to show
                    OnApplyRedirect = delegate(GoogleOAuth2ApplyRedirectContext context)
                    {
                        string redirect = context.RedirectUri;
                        redirect += "&prompt=select_account";
                        context.Response.Redirect(redirect);
                    },

                    // Add claims to name, email and google access token
                    OnAuthenticated = (context) =>
                    {
                        context.Identity.AddClaim(new Claim("urn:google:name", context.Identity.FindFirstValue(ClaimTypes.Name)));
                        context.Identity.AddClaim(new Claim("urn:google:email", context.Identity.FindFirstValue(ClaimTypes.Email)));
                        //This following line is need to retrieve the profile image
                        context.Identity.AddClaim(new Claim("urn:google:accesstoken", context.AccessToken, ClaimValueTypes.String, "Google"));
                       
                        return System.Threading.Tasks.Task.FromResult(0);
                    }
                }
            };
            
            app.UseGoogleAuthentication(googleAuthOptions);

            app.UseOpenIDAuthentication("https://appleid.apple.com/auth/authorize", "Apple", true);

        }
    }
}
