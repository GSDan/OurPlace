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
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using OurPlace.Common;
using QRCoder;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace OurPlace.API
{
    public static class ServerUtils
    {
        private static CloudBlobClient blobClient;
        private static CloudBlobContainer appContainer;

        public static CloudBlobContainer GetCloudBlobContainer()
        {
            if(appContainer == null)
            {
                if(blobClient == null)
                {
                    var accountName = ConfigurationManager.AppSettings["storage:account:name"];
                    var accountKey = ConfigurationManager.AppSettings["storage:account:key"];
                    var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
                    blobClient = storageAccount.CreateCloudBlobClient();
                }

                appContainer = blobClient.GetContainerReference("parklearn");
                appContainer.CreateIfNotExists();
            }

            return appContainer;
        }

        public struct DownloadStruct
        {
            public CloudBlockBlob Blob { get; set; }
            public string Filename { get; set; }
        }

        public static void ZipFilesToResponse(HttpResponseBase response, string filename, ICollection<DownloadStruct> files)
        {
            byte[] buffer = new byte[4096];
            response.ContentType = "application/zip";

            response.AppendHeader("content-disposition", "attachment; filename=\"" + filename + ".zip\"");
            response.CacheControl = "Private";
            response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(30));

            ZipOutputStream zipOutputStream = new ZipOutputStream(response.OutputStream);
            zipOutputStream.SetLevel(3); //0-9, 9 being the highest level of compression
            
            foreach(DownloadStruct file in files)
            {
                Stream fs = file.Blob.OpenRead();
                ZipEntry entry = new ZipEntry(ZipEntry.CleanName(file.Filename));
                entry.Size = fs.Length;

                zipOutputStream.PutNextEntry(entry);

                int count = fs.Read(buffer, 0, buffer.Length);
                while (count > 0)
                {
                    zipOutputStream.Write(buffer, 0, count);
                    count = fs.Read(buffer, 0, buffer.Length);
                    if (!response.IsClientConnected)
                    {
                        break;
                    }
                    response.Flush();
                }
                fs.Close();
            }
            zipOutputStream.Close();
            response.Flush();
            response.End();
        }

        public static string GetActivityShareUrl(string shareCode)
        {
            return ConfidentialData.api + "app/activity?code=" + shareCode;
        }

        public static string GetResultShareUrl(string shareCode)
        {
            return ConfidentialData.api + "CompletedTasks?code=" + shareCode;
        }
        
        public static Bitmap GenerateQRCode(string toEncode, bool includeLogo = true)
        {
            QRCodeGenerator generator = new QRCodeGenerator();
            QRCodeData qrCodeData = generator.CreateQrCode(toEncode, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);

            if(includeLogo)
            {
                using (WebClient wc = new WebClient())
                {
                    using (Stream s = wc.OpenRead(ConfidentialData.smallLogoUrl))
                    {
                        return qrCode.GetGraphic(20, Color.Black, Color.White, new Bitmap(s));
                    }
                }
            }
            else
            {
                return qrCode.GetGraphic(20, Color.Black, Color.White, true);
            }
        }

        public static async Task<Response> SendEmail(string[] toEmail, string subject, string content, bool isHtml)
        {
            EmailAddress fromAddress = new EmailAddress("d.richardson@newcastle.ac.uk", "Dan at OurPlace");

            SendGridMessage message = new SendGridMessage();
            message.SetFrom(fromAddress);
            message.AddTo(fromAddress);

            foreach(string address in toEmail)
            {
                message.AddBcc(address);
            }

            message.SetSubject(subject);
            message.AddContent(isHtml ? MimeType.Html : MimeType.Text, content);

            // Key is hosted on Azure Portal, in application settings
            string apiKey = Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
            SendGridClient client = new SendGridClient(apiKey);
            return await client.SendEmailAsync(message);
        }


        public static class StringCipher
        {
            // This constant is used to determine the keysize of the encryption algorithm in bits.
            // We divide this by 8 within the code below to get the equivalent number of bytes.
            private const int Keysize = 256;

            // This constant determines the number of iterations for the password bytes generation function.
            private const int DerivationIterations = 1000;

            public static string Encrypt(string plainText, string passPhrase)
            {
                // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
                // so that the same Salt and IV values can be used when decrypting.  
                var saltStringBytes = Generate256BitsOfRandomEntropy();
                var ivStringBytes = Generate256BitsOfRandomEntropy();
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                    cryptoStream.FlushFinalBlock();
                                    // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                    var cipherTextBytes = saltStringBytes;
                                    cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                    cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Convert.ToBase64String(cipherTextBytes);
                                }
                            }
                        }
                    }
                }
            }

            public static string Decrypt(string cipherText, string passPhrase)
            {
                // Get the complete stream of bytes that represent:
                // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
                var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
                // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
                var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
                // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
                var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
                // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
                var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream(cipherTextBytes))
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                {
                                    var plainTextBytes = new byte[cipherTextBytes.Length];
                                    var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                }
                            }
                        }
                    }
                }
            }

            private static byte[] Generate256BitsOfRandomEntropy()
            {
                var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
                using (var rngCsp = new RNGCryptoServiceProvider())
                {
                    // Fill the array with cryptographically secure random bytes.
                    rngCsp.GetBytes(randomBytes);
                }
                return randomBytes;
            }
        }
    }
}