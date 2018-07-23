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
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler.Serializer;
using Microsoft.Owin.Security.Infrastructure;
using OurPlace.API.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Security;

namespace OurPlace.API.Providers
{
    // https://alexdunn.org/2015/04/30/adding-a-simple-refresh-token-to-oauth-bearer-tokens/
    public class RefreshTokenProvider : IAuthenticationTokenProvider
    {
        // for debugging in memory
        //List<RefreshToken> tokens = new List<RefreshToken>();

        public async Task CreateAsync(AuthenticationTokenCreateContext context)
        {
            var form = context.Request.ReadFormAsync().Result;
            var grantType = form.GetValues("grant_type");
            
            // don't create a new refresh token if the user is only after a new access token
            if(grantType == null || grantType[0] != "refresh_token" )
            {
                var db = context.OwinContext.Get<ApplicationDbContext>();
                var userManager = context.OwinContext.GetUserManager<ApplicationUserManager>();

                Random rand = new Random();
                string tokenString = Membership.GeneratePassword(30, 0);
                tokenString = Regex.Replace(tokenString, @"[^a-zA-Z0-9]", m => rand.Next(0, 10).ToString());

                DateTimeOffset issuedUtc = DateTimeOffset.UtcNow;
                DateTimeOffset expiresUtc = DateTimeOffset.UtcNow.AddYears(1);

                AuthenticationTicket refreshTokenTicket = new AuthenticationTicket(
                    context.Ticket.Identity, 
                    new AuthenticationProperties(context.Ticket.Properties.Dictionary));

                refreshTokenTicket.Properties.IssuedUtc = issuedUtc;
                refreshTokenTicket.Properties.ExpiresUtc = expiresUtc;

                TicketSerializer serializer = new TicketSerializer();
                string serializedTicket = Convert.ToBase64String(serializer.Serialize(refreshTokenTicket));

                // Store in the database, with the hashed token and the ticket as encrypted 
                // json using the token as the decrypt password
                RefreshToken newToken = new RefreshToken
                {
                    CreatedAtUtc = issuedUtc,
                    ExpiresAtUtc = expiresUtc,
                    Revoked = false,
                    User = await userManager.FindByNameAsync(context.Ticket.Identity.Name),
                    TokenHash = GetStringSha256Hash(tokenString),
                    EncryptedTicket = ServerUtils.StringCipher.Encrypt(serializedTicket, tokenString)
                };
                db.RefreshTokens.Add(newToken);
                await db.SaveChangesAsync();

                //tokens.Add(newToken);

                context.SetToken(tokenString);
            }
        }

        public async Task ReceiveAsync(AuthenticationTokenReceiveContext context)
        {
            var db = context.OwinContext.Get<ApplicationDbContext>();
            string hash = GetStringSha256Hash(context.Token);

            RefreshToken foundToken = db.RefreshTokens.Where(t =>
                t.TokenHash == hash &&
                t.ExpiresAtUtc > DateTime.UtcNow &&
                !t.Revoked
            ).FirstOrDefault();

            //RefreshToken foundToken = tokens.Where(t =>
            //    t.TokenHash == hash &&
            //    t.ExpiresAtUtc > DateTime.UtcNow &&
            //    !t.Revoked
            //).FirstOrDefault();

            if (foundToken != null)
            {
                byte[] serializedTicket = Convert.FromBase64String(
                    ServerUtils.StringCipher.Decrypt(
                        foundToken.EncryptedTicket,
                        context.Token));
                TicketSerializer serializer = new TicketSerializer();
                AuthenticationTicket ticket = serializer.Deserialize(serializedTicket);

                context.SetTicket(ticket);
            }
        }

        // https://stackoverflow.com/questions/3984138/hash-string-in-c-sharp
        internal static string GetStringSha256Hash(string text)
        {
            if (String.IsNullOrEmpty(text))
                return String.Empty;

            using (var sha = new SHA256Managed())
            {
                byte[] textData = Encoding.UTF8.GetBytes(text);
                byte[] hash = sha.ComputeHash(textData);
                return BitConverter.ToString(hash).Replace("-", String.Empty);
            }
        }

        public static DateTimeOffset? GetExpiryDate(string token, IOwinContext context)
        {
            var db = context.Get<ApplicationDbContext>();
            string hash = GetStringSha256Hash(token);

            RefreshToken foundToken = db.RefreshTokens.Where(t =>
                t.TokenHash == hash 
            ).FirstOrDefault();

            if (foundToken != null)
            {
                return foundToken.ExpiresAtUtc;
            }
            return null;
        }

        public void Create(AuthenticationTokenCreateContext context)
        {
            throw new NotImplementedException();
        }

        public void Receive(AuthenticationTokenReceiveContext context)
        {
            throw new NotImplementedException();
        }
    }
}
