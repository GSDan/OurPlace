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
using Facebook;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security.Facebook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace ParkLearn.API.Providers
{
    public class FacebookAuthProvider : FacebookAuthenticationProvider
    {
        public override Task Authenticated(FacebookAuthenticatedContext context)
        {
            var accessTokenClaim = new Claim("ExternalAccessToken", context.AccessToken, "urn:facebook:access_token");
            context.Identity.AddClaim(accessTokenClaim);
            var extraClaims = GetAdditionalFacebookClaims(accessTokenClaim);
            context.Identity.AddClaim(new Claim(ClaimTypes.Email, extraClaims.First(k => k.Key == "email").Value.ToString()));
            context.Identity.AddClaim(new Claim("Provider", context.Identity.AuthenticationType));
            context.Identity.AddClaim(new Claim(ClaimTypes.Name, context.Identity.FindFirstValue(ClaimTypes.Name)));

            var userDetail = context.User;
            var link = userDetail.Value<string>("link") ?? string.Empty;
            context.Identity.AddClaim(new Claim("link", link));
            context.Identity.AddClaim(new Claim("FacebookId", userDetail.Value<string>("id")));
            return System.Threading.Tasks.Task.FromResult(0);
        }

        private static JsonObject GetAdditionalFacebookClaims(Claim accessToken)
        {
            var fb = new FacebookClient(accessToken.Value);
            return fb.Get("me", new { fields = new[] { "email", "first_name", "last_name" } }) as JsonObject;
        }
    }
}