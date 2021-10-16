using FanaticalLibrary.Models;
using Playnite.Common;
using Playnite.SDK;
using Playnite.SDK.Data;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FanaticalLibrary.Services
{
    public class TokenException : Exception
    {
        public TokenException(string message) : base(message)
        {
        }
    }


    public class FanaticalAccountClient
    {
        private ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI api;
        private string tokensPath;
        private readonly string loginUrl = "https://www.fanatical.com/en";
        private readonly string accountUrl = "https://www.fanatical.com/en/account";
        private readonly string gamesUrl = "https://www.fanatical.com/api/user/keys";

        private static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };

        private static HttpClient httpClient = new HttpClient(handler){};

        public FanaticalAccountClient(IPlayniteAPI api, string tokensPath)
        {
            this.api = api;
            this.tokensPath = tokensPath;

            //var loadedFromConfig = false;
        }

        public void Login()
        {
            var loggedIn = false;
            var apiRedirectContent = string.Empty;
            String authToken;

            /*//TODO get token managing login web browser windows
            using (var view = api.WebViews.CreateView(580, 700))
            {
                //TODO 7navigation and authentication.
                //view.DeleteDomainCookies(".fanatical.com");
                view.LoadingChanged += async (s, e) =>
                {
                    var address = view.GetCurrentAddress();
                    if (address.StartsWith(loginUrl))
                    {
                        apiRedirectContent = await view.GetPageTextAsync();
                        loggedIn = true;
                        view.Close();
                    }
                };

                view.Navigate(loginUrl);
                view.OpenDialog();
            

            if (!loggedIn)
            {
                return;
            }

            FileSystem.DeleteFile(tokensPath);

             //TODO Execute script to get auth token
            authToken = view.EvaluateScriptAsync("window.localStorage.getItem(\"bsauth\");").Result.ToString();
            }

            if (string.IsNullOrEmpty(authToken))
            {
                logger.Error("Failed to get login token for fanatical account.");
                return;
            }


            */

            try
            { 

            //authToken = "{\"sentEmail\":false,\"authenticated\":true,\"email\":\"rvanzo1971 @gmail.com\",\"error\":null,\"challenge\":null,\"magicSuccess\":null,\"magicSummoned\":null,\"_id\":\"58d301b9dc0d8214008dca83\",\"role\":\"customer\",\"created\":\"2017 - 03 - 22T22: 59:05.252Z\",\"language\":{\"code\":\"en\",\"label\":\"English\",\"nativeLabel\":\"English\"},\"email_confirmed\":true,\"twoFactorEnabled\":false,\"email_newsletter\":true,\"steam\":{},\"epic\":{},\"wishlist_notifications\":true,\"cart_notifications\":true,\"review_reminders\":true,\"user_review_reminders\":true,\"date_last_email_redeem_confirm\":false,\"alreadyHasAccount\":false,\"billing\":{\"customerName\":null,\"address1\":null,\"address2\":null,\"locality\":null,\"administrativeArea\":null,\"postalCode\":null,\"countryCode\":null},\"token\":\"58d301b9dc0d8214008dca83.cca42250 - 05c5 - 4f39 - ae87 - 4b635c787a3d\"}";
            authToken = "{\"sentEmail\":false,\"authenticated\":true,\"email\":\"rvanzo1971 @gmail.com\",\"error\":null,\"challenge\":null,\"magicSuccess\":null,\"magicSummoned\":null,\"_id\":\"58d301b9dc0d8214008dca83\",\"role\":\"customer\",\"created\":\"2017 - 03 - 22T22: 59:05.252Z\",\"language\":\"en-EN\",\"email_confirmed\":true,\"twoFactorEnabled\":false,\"email_newsletter\":true,\"steam\":\"\",\"epic\":\"\",\"wishlist_notifications\":true,\"cart_notifications\":true,\"review_reminders\":true,\"user_review_reminders\":true,\"date_last_email_redeem_confirm\":false,\"alreadyHasAccount\":false,\"billing\":\"customerName\",\"token\":\"58d301b9dc0d8214008dca83.cca42250 - 05c5 - 4f39 - ae87 - 4b635c787a3d\"}";

                FileSystem.CreateDirectory(Path.GetDirectoryName(tokensPath));
            Encryption.EncryptToFile(
                tokensPath,
                authToken,
                Encoding.UTF8,
                WindowsIdentity.GetCurrent().User.Value);

                loggedIn = true;
            }

            catch (Exception e)
            {
                logger.Error(e, "Failed to write token.");
            }

        }

        public bool GetIsUserLoggedIn()
        {
            var token = getToken();

            if (token == null)
            {
                return false;
            }
/* //TODO Token validation on accout url
            try
            {
                var account = InvokeRequest<AccountResponse>(accountUrl + tokens.account_id, tokens).GetAwaiter().GetResult().Item2;
                return account.id == tokens.account_id;
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to validation Fanatical authentication.");
                return false;
            }
*/
           return true;
        }

        public List<FanaticalLibraryItem> GetLibraryItems()
        {
            if (!GetIsUserLoggedIn())
            {
                throw new Exception("User is not authenticated.");
            }
            /*
            List<FanaticalLibraryItem> AllGames=null;

            SetHeaders(httpClient.DefaultRequestHeaders);

            try
            {
                var response = httpClient.GetAsync(gamesUrl, HttpCompletionOption.ResponseContentRead);

                var str = response.GetAwaiter().GetResult().Content.ToString();

                AllGames = Serialization.FromJson<List<FanaticalLibraryItem>>(str);// && !string.IsNullOrEmpty(error.errorCode))

            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to get games.");
                return null;
            }

            return AllGames;
            */

            var jsonResponse = InvokeAuthenticatedRequest(gamesUrl).GetAwaiter().GetResult(); //TODO

            return Serialization.FromJson<List<FanaticalLibraryItem>>(jsonResponse); //TODO
        }

        private async Task<string> InvokeAuthenticatedRequest(string url) 
        {
            string str;

            SetHeaders(httpClient.DefaultRequestHeaders);

            try
            {
                var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead);

                if (response.StatusCode == HttpStatusCode.OK) { 
                    str = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("HTTP request fail with error code "+ response.StatusCode);
                }


            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to get games from web.");
                return null;
            }

            return str;

/*
            if (Serialization.TryFromJson<ErrorResponse>(str, out var error) && !string.IsNullOrEmpty(error.errorCode))
                {
                    throw new TokenException(error.errorCode);
                }
                else
                {
                    return new Tuple<string, T>(str, Serialization.FromJson<T>(str));
                }
  */       //   }
        }

        private void SetHeaders(HttpRequestHeaders headers)
        {
            headers.Clear();

            headers.Accept.ParseAdd("application/json");
            headers.Add("authorization", getToken());
            headers.AcceptEncoding.ParseAdd("gzip,deflate"); 
            headers.AcceptLanguage.ParseAdd("en-GB,en-us;q=0.8,en;q=0.6");
            headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
            //headers.Connection.TryParseAdd("keep-alive");
            //headers.Host = new Uri(LoginUrl).Host;
            //headers.Add("X-Requested-With", "XMLHttpRequest");
        }


        private String getToken()
        {
            if (File.Exists(tokensPath))
            {
                try
                {

                    var str = Encryption.DecryptFromFile(
                            tokensPath,
                            Encoding.UTF8,
                            WindowsIdentity.GetCurrent().User.Value);

                    return Serialization.FromJson <FanaticalToken>(str).token.Replace(" ", string.Empty);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Failed to load saved tokens.");
                }
            }

            return null;
        }

    }
}