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
using AppleAuth;
using AppleAuth.TokenObjects;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Infrastructure;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json;
using OurPlace.API.Models;
using OurPlace.API.Providers;
using OurPlace.API.Results;
using ParkLearn.PCL.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace OurPlace.API.Controllers
{
    [Authorize]
    [RequireHttps]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        private const string LocalLoginProvider = "Local";
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager,
            ISecureDataFormat<AuthenticationTicket> accessTokenFormat)
        {
            UserManager = userManager;
            AccessTokenFormat = accessTokenFormat;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ISecureDataFormat<AuthenticationTicket> AccessTokenFormat { get; private set; }

        // GET api/Account/UserInfo
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("UserInfo")]
        public UserInfoViewModel GetUserInfo()
        {
            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);
            ApplicationUser user = UserManager.FindById(User.Identity.GetUserId());

            return new UserInfoViewModel
            {
                Email = User.Identity.GetUserName(),
                HasRegistered = externalLogin == null,
                LoginProvider = externalLogin?.LoginProvider
            };
        }

        // Get api/Account/
        [Route("")]
        public async Task<AccountViewModel> GetMyAccount()
        {
            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            return new AccountViewModel
            {
                Id = user.Id,
                Email = user.Email,
                DateCreated = user.DateCreated,
                FirstName = user.FirstName,
                Surname = user.Surname,
                ImageUrl = user.ImageUrl,
                Trusted = user.Trusted
            };
        }

        // POST api/Account/Logout
        [Route("Logout")]
        public IHttpActionResult Logout()
        {
            Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            return Ok();
        }

        // GET api/Account/ManageInfo?returnUrl=%2F&generateState=true
        [Route("ManageInfo")]
        public async Task<ManageInfoViewModel> GetManageInfo(string returnUrl, bool generateState = false)
        {
            IdentityUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            if (user == null)
            {
                return null;
            }

            List<UserLoginInfoViewModel> logins = new List<UserLoginInfoViewModel>();

            foreach (IdentityUserLogin linkedAccount in user.Logins)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = linkedAccount.LoginProvider,
                    ProviderKey = linkedAccount.ProviderKey
                });
            }

            if (user.PasswordHash != null)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = LocalLoginProvider,
                    ProviderKey = user.UserName,
                });
            }

            return new ManageInfoViewModel
            {
                LocalLoginProvider = LocalLoginProvider,
                Email = user.UserName,
                Logins = logins,
                ExternalLoginProviders = GetExternalLogins(returnUrl, generateState)
            };
        }

        // POST api/Account/ChangePassword
        [Route("ChangePassword")]
        public async Task<IHttpActionResult> ChangePassword(ChangePasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword,
                model.NewPassword);
            
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/SetPassword
        [Route("SetPassword")]
        public async Task<IHttpActionResult> SetPassword(SetPasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/AddExternalLogin
        [Route("AddExternalLogin")]
        public async Task<IHttpActionResult> AddExternalLogin(AddExternalLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            AuthenticationTicket ticket = AccessTokenFormat.Unprotect(model.ExternalAccessToken);

            if (ticket == null || ticket.Identity == null || (ticket.Properties != null
                && ticket.Properties.ExpiresUtc.HasValue
                && ticket.Properties.ExpiresUtc.Value < DateTimeOffset.UtcNow))
            {
                return BadRequest("External login failure.");
            }

            ExternalLoginData externalData = ExternalLoginData.FromIdentity(ticket.Identity);

            if (externalData == null)
            {
                return BadRequest("The external login is already associated with an account.");
            }

            IdentityResult result = await UserManager.AddLoginAsync(User.Identity.GetUserId(),
                new UserLoginInfo(externalData.LoginProvider, externalData.ProviderKey));

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/RemoveLogin
        [Route("RemoveLogin")]
        public async Task<IHttpActionResult> RemoveLogin(RemoveLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result;

            if (model.LoginProvider == LocalLoginProvider)
            {
                result = await UserManager.RemovePasswordAsync(User.Identity.GetUserId());
            }
            else
            {
                result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(),
                    new UserLoginInfo(model.LoginProvider, model.ProviderKey));
            }

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // https://stackoverflow.com/questions/25739710/how-to-create-refresh-token-with-external-login-provider
        private async Task<AuthenticationProperties> AddRefreshToken(ClaimsIdentity oAuthIdentity, AuthenticationProperties properties)
        {
            try
            {
                AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);

                AuthenticationTokenCreateContext context = new AuthenticationTokenCreateContext(
                    Request.GetOwinContext(),
                    Startup.OAuthOptions.AccessTokenFormat,
                    ticket);

                await Startup.OAuthOptions.RefreshTokenProvider.CreateAsync(context);
                DateTimeOffset? refreshExpires = RefreshTokenProvider.GetExpiryDate(context.Token, Request.GetOwinContext());
                properties.Dictionary.Add("refresh_token", context.Token);
                properties.Dictionary.Add("refresh_token_expires", (refreshExpires == null) ? DateTime.UtcNow.Ticks.ToString() : ((DateTimeOffset)refreshExpires).UtcTicks.ToString());

                return properties;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        // GET api/Account/ExternalLogin
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalCookie)]
        [AllowAnonymous]
        [Route("ExternalLogin", Name = "ExternalLogin")]
        public async Task<IHttpActionResult> GetExternalLogin(string provider, string redirect_uri, string error = null)
        {
            if (error != null)
            {
                return Redirect(Url.Content("~/") + "#error=" + Uri.EscapeDataString(error));
            }

            if (provider == "Apple")
            {
                Console.WriteLine(redirect_uri);

                return Redirect(string.Format("https://appleid.apple.com/auth/authorize?client_id={0}&response_type=code&response_mode=form_post&scope={1}&redirect_uri={2}&state={3}",
                    ConfigurationManager.AppSettings["oauth:apple:id"],
                    Uri.EscapeDataString("name email"),
                    string.Format("https://{0}/api/account/HandleResponseFromApple", Request.RequestUri.Authority),
                    //"https://webhook.site/cb4691d4-fdaf-4250-8613-a9ed1623453b",
                    redirect_uri));
            }

            if (!User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(provider, this);
            }

            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            if (externalLogin == null)
            {
                return InternalServerError();
            }

            if (externalLogin.LoginProvider != provider)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                return new ChallengeResult(provider, this);
            }

            ApplicationUser user = await UserManager.FindAsync(new UserLoginInfo(externalLogin.LoginProvider,
                externalLogin.ProviderKey));

            bool hasRegistered = user != null;

            if (hasRegistered)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                
                 ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(UserManager,
                    OAuthDefaults.AuthenticationType);
                ClaimsIdentity cookieIdentity = await user.GenerateUserIdentityAsync(UserManager,
                    CookieAuthenticationDefaults.AuthenticationType);

                user.LastConsent = DateTime.UtcNow;
                await UserManager.UpdateAsync(user);

                AuthenticationProperties properties = ApplicationOAuthProvider.CreateProperties(user.UserName);
                properties = await AddRefreshToken(oAuthIdentity, properties);
                Authentication.SignIn(properties, oAuthIdentity, cookieIdentity);
            }
            else
            {
                RegisterExternalBindingModel model = new RegisterExternalBindingModel();

                if (externalLogin.LoginProvider == "Facebook")
                {
                    // Get details from Facebook
                    var accessToken = Authentication.User.Claims.Where(c => c.Type.Equals("ExternalAccessToken")).Select(c => c.Value).FirstOrDefault();
                    Uri apiRequestUri = new Uri("https://graph.facebook.com/me?fields=id,first_name,last_name,email,picture.type(square)&access_token=" + accessToken);

                    /* request facebook profile
                     * JSON return format:
                    {
                      "id": "1******4",
                      "first_name": "Dan",
                      "last_name": "Richardson",
                      "email": "****", not guaranteed 
                      "picture": {
                        "data": {
                          "height": 50,
                          "is_silhouette": false,
                          "url": "https://******",
                          "width": 50
                        }
                      }
                    }*/
                    using (var webClient = new WebClient())
                    {
                        var json = webClient.DownloadString(apiRequestUri);
                        dynamic jsonResult = JsonConvert.DeserializeObject(json);
                        model.Email = jsonResult.email;
                        model.Given_name = jsonResult.first_name;
                        model.Family_name = jsonResult.last_name;
                        model.Picture = jsonResult.picture.data.url;
                    }
                }
                else
                {
                    var accessToken = Authentication.User.Claims.Where(c => c.Type.Equals("urn:google:accesstoken")).Select(c => c.Value).FirstOrDefault();
                    Uri apiRequestUri = new Uri("https://www.googleapis.com/oauth2/v2/userinfo?access_token=" + accessToken);

                    //request Google profile
                    /* Json result format:
                     * {{
                          "id": "1****************6",
                          "email": "dan********@gmail.com",
                          "verified_email": true,
                          "name": "Dan Richardson",
                          "given_name": "Dan",
                          "family_name": "Richardson",
                          "link": "https://plus.google.com/+DanRichardson",
                          "picture": "https://lh4.googleusercontent.com/-7crdql0UPAU/AAAAAAAAAAI/AAAAAAAAVbU/Ho345LtUt8M/photo.jpg",
                          "gender": "male",
                          "locale": "en"
                        }}
                        */

                    using (var webClient = new WebClient())
                    {
                        var json = webClient.DownloadString(apiRequestUri);
                        dynamic jsonResult = JsonConvert.DeserializeObject(json);
                        model.Email = jsonResult.email;
                        model.Picture = jsonResult.picture;
                        model.Family_name = jsonResult.family_name;
                        model.Given_name = jsonResult.given_name;
                    }
                }

                var appUser = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    ImageUrl = model.Picture,
                    FirstName = model.Given_name,
                    Surname = model.Family_name,
                    DateCreated = DateTime.UtcNow,
                    AuthProvider = externalLogin.LoginProvider,
                    Trusted = false,
                    LastConsent = DateTime.UtcNow
                };

                var loginInfo = await Authentication.GetExternalLoginInfoAsync();

                ApplicationUser existingResult = await UserManager.FindByEmailAsync(appUser.Email);

                if(existingResult == null)
                {
                    // New user
                    IdentityResult createResult = await UserManager.CreateAsync(appUser);
                    if (!createResult.Succeeded)
                    {
                        return GetErrorResult(createResult);
                    }
                }
                else
                {
                    // user already exists with that email
                    appUser.Id = existingResult.Id;
                    await UserManager.UpdateAsync(appUser);
                }

                // Add this oauth login to the user account
                IdentityResult result = await UserManager.AddLoginAsync(appUser.Id, loginInfo.Login);
                if (!result.Succeeded)
                {
                    return GetErrorResult(result);
                }

                ClaimsIdentity oAuthIdentity = await appUser.GenerateUserIdentityAsync(UserManager,
                   OAuthDefaults.AuthenticationType);
                ClaimsIdentity cookieIdentity = await appUser.GenerateUserIdentityAsync(UserManager,
                    CookieAuthenticationDefaults.AuthenticationType);

                AuthenticationProperties properties = ApplicationOAuthProvider.CreateProperties(appUser.UserName);
                properties = await AddRefreshToken(oAuthIdentity, properties);
                Authentication.SignIn(properties, oAuthIdentity, cookieIdentity);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogins?returnUrl=%2F&generateState=true
        [AllowAnonymous]
        [Route("ExternalLogins")]
        public IEnumerable<ExternalLoginViewModel> GetExternalLogins(string returnUrl, bool generateState = false)
        {
            IEnumerable<AuthenticationDescription> descriptions = Authentication.GetExternalAuthenticationTypes();
            List<ExternalLoginViewModel> logins = new List<ExternalLoginViewModel>();

            string state;

            if (generateState)
            {
                const int strengthInBits = 256;
                state = RandomOAuthStateGenerator.Generate(strengthInBits);
            }
            else
            {
                state = null;
            }

            foreach (AuthenticationDescription description in descriptions)
            {
                string url = Url.Route("ExternalLogin", new
                {
                    provider = description.AuthenticationType,
                    response_type = "token",
                    client_id = Startup.PublicClientId,
                    redirect_uri = new Uri(Request.RequestUri, returnUrl).AbsoluteUri,
                    state = state
                });

                ExternalLoginViewModel login = new ExternalLoginViewModel
                {
                    Name = description.Caption,
                    Url = url,
                    State = state
                };
                logins.Add(login);
            }

            return logins;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IHttpActionResult> HandleResponseFromApple()
        {
            try
            {
                // TODO should probably check state here (and in the non-Apple one)

                InitialTokenResponse response = new InitialTokenResponse();
                response.code = HttpContext.Current.Request.Form["code"];
                response.user = HttpContext.Current.Request.Form["user"];
                response.state = HttpContext.Current.Request.Form["state"]; //use state as client's requested return uri

                // Create a new instance of AppleAuthProvider
                AppleAuthProvider provider = new AppleAuthProvider(
                    ConfigurationManager.AppSettings["oauth:apple:id"],
                    ConfigurationManager.AppSettings["oauth:apple:teamid"],
                    ConfigurationManager.AppSettings["oauth:apple:keyid"],
                    //"https://webhook.site/cb4691d4-fdaf-4250-8613-a9ed1623453b",//
                    string.Format("https://{0}/api/account/HandleResponseFromApple", Request.RequestUri.Authority),
                    response.state);
                // Retrieve an authorization token
                AuthorizationToken authorizationToken = await provider.GetAuthorizationToken(response.code, ConfigurationManager.AppSettings["oauth:apple:secret"]);

                var login = new UserLoginInfo("Apple", authorizationToken.UserInformation.UserID);
                ApplicationUser user = await UserManager.FindAsync(login);

                if(user == null && string.IsNullOrWhiteSpace(response.user))
                {
                    // new user but apple didn't give details
                    return InternalServerError(new Exception("Something went wrong while getting your details from Apple. Please remove OurPlace from your 'Sign in with Apple' security settings and try again."));
                }
                
                // apple only returns the user's details on the first authentication
                if (user == null && !string.IsNullOrWhiteSpace(response.user))
                {
                    dynamic appleUser = JsonConvert.DeserializeObject(response.user);

                    user = new ApplicationUser
                    {
                        UserName = appleUser.email,
                        Email = appleUser.email,
                        FirstName = appleUser.name.firstName,
                        Surname = appleUser.name.lastName,
                        DateCreated = DateTime.UtcNow,
                        AuthProvider = "Apple",
                        Trusted = false,
                        LastConsent = DateTime.UtcNow
                    };

                    ApplicationUser existingResult = await UserManager.FindByEmailAsync(user.Email);

                    if (existingResult == null)
                    {
                        // New user
                        IdentityResult createResult = await UserManager.CreateAsync(user);
                        if (!createResult.Succeeded)
                        {
                            return GetErrorResult(createResult);
                        }
                    }
                    else
                    {
                        // user already exists with that email
                        user.Id = existingResult.Id;
                        await UserManager.UpdateAsync(user);
                    }

                    // Add this oauth login to the user account
                    IdentityResult idResult = await UserManager.AddLoginAsync(user.Id, login);
                    if (!idResult.Succeeded)
                    {
                        return GetErrorResult(idResult);
                    }
                }
                else
                {
                    // existing user
                    Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                }

                ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(UserManager, OAuthDefaults.AuthenticationType);
                ClaimsIdentity cookieIdentity = await user.GenerateUserIdentityAsync(UserManager, CookieAuthenticationDefaults.AuthenticationType);

                AuthenticationProperties properties = ApplicationOAuthProvider.CreateProperties(user.UserName);
                properties = await AddRefreshToken(oAuthIdentity, properties);
                Authentication.SignIn(properties, oAuthIdentity, cookieIdentity);

                user.LastConsent = DateTime.UtcNow;
                await UserManager.UpdateAsync(user);

                Dictionary<string, string> args = new Dictionary<string, string>()
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", properties.Dictionary["refresh_token"] }
                };
                RefreshResponse refResp;

                using (var client = new HttpClient())
                {
                    HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, string.Format("https://{0}/Token", Request.RequestUri.Authority))
                    {
                        Content = new FormUrlEncodedContent(args)
                    };
                    HttpResponseMessage resp = await client.SendAsync(req);
                    if (resp.Content == null || !resp.IsSuccessStatusCode)
                    {
                        throw new Exception("Couldn't get an access token");
                    }
                    string json = await resp.Content.ReadAsStringAsync();
                    refResp = JsonConvert.DeserializeObject<RefreshResponse>(json);
                }

                return Redirect(string.Format("{0}#access_token={1}&token_type=bearer&expires_in={2}&refresh_token={3}&refresh_token_expires={4}",
                    response.state,
                    refResp.Access_token,
                    refResp.Expires_in,
                    properties.Dictionary["refresh_token"],
                    properties.Dictionary["refresh_token_expires"]));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return InternalServerError(new Exception(string.Format("HandleResponseFromApple\nkey{2}\n{0}\n{1}", e.Message, e.StackTrace, ConfigurationManager.AppSettings["oauth:apple:secret"])));
            }
        }

        // POST api/Account/Register
        [AllowAnonymous]
        [Route("Register")]
        public async Task<IHttpActionResult> Register(RegisterBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser() { UserName = model.Email, Email = model.Email };

            IdentityResult result = await UserManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }


        private async Task<ExternalLoginInfo> AuthenticationManager_GetExternalLoginInfoAsync_WithExternalBearer()
        {
            ExternalLoginInfo loginInfo = null;

            var result = await Authentication.AuthenticateAsync(DefaultAuthenticationTypes.ExternalBearer);

            if (result != null && result.Identity != null)
            {
                var idClaim = result.Identity.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim != null)
                {
                    loginInfo = new ExternalLoginInfo()
                    {
                        DefaultUserName = result.Identity.Name == null ? "" : result.Identity.Name.Replace(" ", ""),
                        Login = new UserLoginInfo(idClaim.Issuer, idClaim.Value),
                        ExternalIdentity = result.Identity
                    };
                }
            }
            return loginInfo;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }

            public IList<Claim> GetClaims()
            {
                IList<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

                if (UserName != null)
                {
                    claims.Add(new Claim(ClaimTypes.Email, UserName, null, LoginProvider));
                }

                return claims;
            }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
            {
                if (identity == null)
                {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
                    || String.IsNullOrEmpty(providerKeyClaim.Value))
                {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                {
                    return null;
                }

                return new ExternalLoginData
                {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Email)
                };
            }
        }

        private static class RandomOAuthStateGenerator
        {
            private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

            public static string Generate(int strengthInBits)
            {
                const int bitsPerByte = 8;

                if (strengthInBits % bitsPerByte != 0)
                {
                    throw new ArgumentException("strengthInBits must be evenly divisible by 8.", "strengthInBits");
                }

                int strengthInBytes = strengthInBits / bitsPerByte;

                byte[] data = new byte[strengthInBytes];
                _random.GetBytes(data);
                return HttpServerUtility.UrlTokenEncode(data);
            }
        }

        #endregion
    }
}
